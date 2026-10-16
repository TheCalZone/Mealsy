const {test} = require('node:test');
const assert = require('node:assert/strict');
const {database} = require('./feed-database');
const {createBilling, entitlementFromPurchase, accountId} = require('../billing');
const {createHandler} = require('../application');
const now = Date.now();
const purchase = (uid = 'owner', state = 'SUBSCRIPTION_STATE_ACTIVE', expiresAt = now + 86400000) => ({
  externalAccountIdentifiers: {obfuscatedExternalAccountId: accountId(uid)}, subscriptionState: state, acknowledgementState: 'ACKNOWLEDGEMENT_STATE_PENDING',
  lineItems: [{productId: 'mealsy_family', offerDetails: {basePlanId: 'annual'}, expiryTime: new Date(expiresAt).toISOString()}]
});
function fixture() {
  const db = database(), records = new Map([['purchase-token-owner', purchase()]]); let acknowledgements = 0;
  const billing = createBilling(db, async token => { if (!records.has(token)) throw Error('Google rejected token'); return structuredClone(records.get(token)); }, async token => { acknowledgements++; records.get(token).acknowledgementState = 'ACKNOWLEDGEMENT_STATE_ACKNOWLEDGED'; }, () => now);
  const call = (uid, data) => billing.handle({auth: uid ? {uid, token: {}} : null, data});
  const household = (uid, data) => createHandler(db)({auth: {uid, token: {email: `${uid}@example.com`}}, data});
  return {db, records, billing, call, household, acks: () => acknowledgements};
}
test('only active, in-grace and cancelled-but-paid subscriptions grant access', () => {
  for (const state of ['SUBSCRIPTION_STATE_ACTIVE','SUBSCRIPTION_STATE_IN_GRACE_PERIOD','SUBSCRIPTION_STATE_CANCELED']) assert(entitlementFromPurchase(purchase('owner', state), 'owner', now).active);
  for (const state of ['SUBSCRIPTION_STATE_PENDING','SUBSCRIPTION_STATE_ON_HOLD','SUBSCRIPTION_STATE_PAUSED','SUBSCRIPTION_STATE_EXPIRED','UNKNOWN']) assert(!entitlementFromPurchase(purchase('owner', state), 'owner', now).active);
  assert(!entitlementFromPurchase(purchase('owner', 'SUBSCRIPTION_STATE_ACTIVE', now - 1), 'owner', now).active);
});
test('purchase must match verified obfuscated account ID, product and annual base plan', () => {
  assert.throws(() => entitlementFromPurchase(purchase('other'), 'owner', now), {code: 'permission-denied'});
  const wrong = purchase(); wrong.lineItems[0].productId = 'other'; assert.throws(() => entitlementFromPurchase(wrong, 'owner', now), {code: 'invalid-argument'});
  const monthly = purchase(); monthly.lineItems[0].offerDetails.basePlanId = 'monthly'; assert.throws(() => entitlementFromPurchase(monthly, 'owner', now), {code: 'invalid-argument'});
});
test('server verifies and acknowledges purchase, retries remain idempotent', async () => {
  const f = fixture(); const plan = await f.call('owner', {action: 'verify', token: 'purchase-token-owner'});
  assert(plan.ownPlan && plan.adFree && plan.familyEnabled); assert.equal(f.acks(), 1);
  await f.call('owner', {action: 'verify', token: 'purchase-token-owner'}); assert.equal(f.acks(), 1);
});
test('invalid or foreign purchase tokens cannot activate another account', async () => {
  const f = fixture(); await assert.rejects(f.call('other', {action: 'verify', token: 'purchase-token-owner'}), {code: 'permission-denied'});
  await assert.rejects(f.call('owner', {action: 'verify', token: 'made-up-invalid-token'}));
  assert(!f.db.docs.has('billingEntitlements/other')); assert(!f.db.docs.has('billingEntitlements/owner'));
});
test('client profile flags and claimed expiry do not unlock free accounts', async () => {
  const f = fixture(); f.db.docs.set('users/free', {premium: true, active: true, expiresAt: now + 10000000});
  assert.equal((await f.call('free', {action: 'status', ownPlan: true})).familyEnabled, false);
  await assert.rejects(f.household('free', {action: 'create', name: 'Free household'}), {code: 'permission-denied'});
});
test('one owner subscription unlocks invited members and removes household ads', async () => {
  const f = fixture(); await f.call('owner', {action: 'verify', token: 'purchase-token-owner'});
  await f.household('owner', {action: 'create', name: 'Family'});
  const {code} = await f.household('owner', {action: 'invite'}); await f.household('member', {action: 'join', code});
  const member = await f.call('member', {action: 'status'}); assert(member.familyEnabled && member.adFree); assert(!member.ownPlan);
});
test('expiry locks shared access and invitations but retains data and allows leaving', async () => {
  const f = fixture(); await f.call('owner', {action: 'verify', token: 'purchase-token-owner'});
  const {householdId} = await f.household('owner', {action: 'create', name: 'Family'});
  const {code} = await f.household('owner', {action: 'invite'}); await f.household('member', {action: 'join', code});
  f.db.docs.set('billingEntitlements/owner', {active: true, expiresAt: now - 1});
  for (const action of ['read','patch']) await assert.rejects(f.household('member', {action, scope: 'household', householdId, changes: []}), {code: 'permission-denied'});
  await assert.rejects(f.household('owner', {action: 'invite'}), {code: 'permission-denied'});
  assert(f.db.docs.has(`households/${householdId}`));
  await f.household('member', {action: 'leave'}); await f.household('owner', {action: 'delete'});
});
test('personal shopping and household cleanup remain free', async () => {
  const f = fixture(); const result = await f.household('free', {action: 'read', scope: 'personal'}); assert.deepEqual(result.items, []);
});
test('refund notification removes family entitlement without trusting event-provided expiry', async () => {
  const f = fixture(); await f.call('owner', {action: 'verify', token: 'purchase-token-owner'});
  f.records.set('purchase-token-owner', purchase('owner', 'SUBSCRIPTION_STATE_EXPIRED', now - 1));
  await f.billing.notification('purchase-token-owner');
  assert.equal((await f.call('owner', {action: 'status'})).adFree, false);
});
test('authentication and purchase token ownership prevent replay', async () => {
  const f = fixture(); await assert.rejects(f.call(null, {action: 'status'}), {code: 'unauthenticated'});
  await f.call('owner', {action: 'verify', token: 'purchase-token-owner'});
  f.records.set('purchase-token-owner', purchase('other'));
  await assert.rejects(f.call('other', {action: 'verify', token: 'purchase-token-owner'}), {code: 'permission-denied'});
});
test('acknowledgement failure keeps the purchase ungranted until restore can succeed', async () => {
  const db = database(); const billing = createBilling(db, async () => purchase(), async () => { throw Error('Play temporarily unavailable'); }, () => now);
  await assert.rejects(billing.verify('owner', 'purchase-token-owner'));
  assert(!db.docs.has('billingEntitlements/owner'));
});
test('an unused invitation cannot be redeemed after the owner plan expires', async () => {
  const f = fixture(); await f.call('owner', {action: 'verify', token: 'purchase-token-owner'});
  await f.household('owner', {action: 'create', name: 'Family'}); const {code} = await f.household('owner', {action: 'invite'});
  f.db.docs.set('billingEntitlements/owner', {active: false, expiresAt: now - 1});
  await assert.rejects(f.household('member', {action: 'join', code}), {code: 'permission-denied'});
  assert(!f.db.docs.has('householdAccounts/member'));
});
