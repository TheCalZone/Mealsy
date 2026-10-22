const {createHash} = require('node:crypto');
const {GoogleAuth} = require('google-auth-library');
const {fail} = require('./domain');
const {active} = require('./entitlements');
const {requestData} = require('./request');
const PACKAGE = 'com.companyname.recipeapp', PRODUCT = 'mealsy_family', BASE_PLAN = 'annual';
const BASE_PLANS = ['annual', 'monthly'];
const SEAT_PRODUCT='mealsy_extra_places';
const sha = value => createHash('sha256').update(value).digest('hex');
const accountId = uid => sha(uid);
const auth = new GoogleAuth({scopes: ['https://www.googleapis.com/auth/androidpublisher']});
async function googlePurchase(token) {
  const client = await auth.getClient();
  return (await client.request({url: `https://androidpublisher.googleapis.com/androidpublisher/v3/applications/${PACKAGE}/purchases/subscriptionsv2/tokens/${encodeURIComponent(token)}`})).data;
}
async function acknowledge(token, productId=PRODUCT) {
  const client = await auth.getClient();
  await client.request({method: 'POST', url: `https://androidpublisher.googleapis.com/androidpublisher/v3/applications/${PACKAGE}/purchases/subscriptions/${productId}/tokens/${encodeURIComponent(token)}:acknowledge`, data: {}});
}
function entitlementFromPurchase(purchase, uid, now = Date.now()) {
  if (purchase.externalAccountIdentifiers?.obfuscatedExternalAccountId !== accountId(uid))
    fail('permission-denied', 'This Google Play subscription belongs to a different Mealsy account. Sign in with the account used to buy it.');
  const lines = (purchase.lineItems || []).filter(item => item.productId === PRODUCT && BASE_PLANS.includes(item.offerDetails?.basePlanId)||item.productId===SEAT_PRODUCT&&/^places-([1-9]|1[0-6])$/.test(item.offerDetails?.basePlanId||''));
  if (!lines.length) fail('invalid-argument', 'This is not a supported Mealsy Family subscription.');
  if(new Set(lines.map(i=>i.productId)).size!==1)fail('invalid-argument','Verify each subscription separately.');
  const selected = lines.reduce((a, b) => (Date.parse(b.expiryTime) || 0) > (Date.parse(a.expiryTime) || 0) ? b : a);
  const expiresAt = Date.parse(selected.expiryTime) || 0;
  const state = purchase.subscriptionState;
  // Cancellation stops renewal but retains access through the paid expiry date.
  const entitled = ['SUBSCRIPTION_STATE_ACTIVE', 'SUBSCRIPTION_STATE_IN_GRACE_PERIOD', 'SUBSCRIPTION_STATE_CANCELED'].includes(state) && expiresAt > now;
  return {active: entitled, expiresAt, state, productId: selected.productId, basePlanId: selected.offerDetails.basePlanId, verifiedAt: now,...(selected.productId===SEAT_PRODUCT?{extraSeats:Number(selected.offerDetails.basePlanId.split('-')[1])}:{})};
}
function createBilling(db, readPurchase = googlePurchase, ack = acknowledge, clock = Date.now) {
  async function verify(uid, token) {
    if (typeof token !== 'string' || token.length < 10 || token.length > 4096) fail('invalid-argument', 'Invalid Google Play purchase token.');
    const ref = db.doc(`billingPurchases/${sha(token)}`);
    const started = clock();
    const purchase = await readPurchase(token);
    const value = {...entitlementFromPurchase(purchase, uid, clock()), verifiedAt: started};
    if (value.active && purchase.acknowledgementState === 'ACKNOWLEDGEMENT_STATE_PENDING') await ack(token,value.productId);
    const entitlementRef=db.doc(`${value.productId===SEAT_PRODUCT?'seatEntitlements':'billingEntitlements'}/${uid}`);
    await db.runTransaction(async tx => {
      if((await tx.get(db.doc(`accountDeletions/${uid}`))).exists)fail('permission-denied','This account is being deleted.');
      const [binding, current] = await Promise.all([tx.get(ref), tx.get(entitlementRef)]);
      if (binding.exists && binding.data().uid !== uid) fail('permission-denied', 'This purchase is already connected to another account.');
      // Reject an out-of-order verification so an old response cannot undo a refund update.
      if (binding.exists && binding.data().verifiedAt > value.verifiedAt) return;
      tx.set(ref, {...value, uid, token});
      // Old replacement tokens must not overwrite the currently-bound subscription.
      if (!current.exists || current.data().tokenHash === ref.id || value.active && value.expiresAt >= current.data().expiresAt)
        tx.set(entitlementRef, {...value, tokenHash: ref.id});
    });
    return value;
  }
  async function refreshOwner(uid) {
    const grant = (await db.doc(`familyGrants/${uid}`).get()).data();
    if (active(grant, clock())) return {...grant, basePlanId: 'admin'};
    const entitlement = (await db.doc(`billingEntitlements/${uid}`).get()).data();
    if (!entitlement?.tokenHash) return null;
    const binding = (await db.doc(`billingPurchases/${entitlement.tokenHash}`).get()).data();
    if (binding?.uid === uid) return verify(uid, binding.token);
    return null;
  }
  async function handle(request) {
    if (!request.auth) fail('unauthenticated', 'Sign in to manage your plan.');
    const uid = request.auth.uid, data = requestData(request, ['verify', 'status']);
    if (data.action === 'verify') await verify(uid, data.token);
    else if (data.action !== 'status') fail('invalid-argument', 'Unknown billing action.');
    const account = (await db.doc(`householdAccounts/${uid}`).get()).data();
    const house = account ? (await db.doc(`households/${account.householdId}`).get()).data() : null;
    const owner = house && Object.hasOwn(house.members || {}, uid) ? house.owner : uid;
    // Refresh both independent personal ownership and household coverage from Play.
    const own = await refreshOwner(uid);
    const family = owner === uid ? own : await refreshOwner(owner);
    let seats=(await db.doc(`seatEntitlements/${owner}`).get()).data();
    if(seats?.tokenHash){const binding=(await db.doc(`billingPurchases/${seats.tokenHash}`).get()).data();if(binding?.uid===owner)seats=await verify(owner,binding.token);}
    return {ownPlan: active(own, clock()), familyEnabled: active(family, clock()), adFree: active(own, clock()) || active(family, clock()),
      expiresAt: Math.max(active(own, clock()) ? own.expiresAt : 0, active(family, clock()) ? family.expiresAt : 0), owner: owner === uid,
      extraSeats:active(seats,clock())?seats.extraSeats:0,productId: PRODUCT, basePlanId: (active(own, clock()) ? own.basePlanId : family?.basePlanId) || null};
  }
  async function notification(token) {
    if (typeof token !== 'string' || token.length > 4096) return;
    const binding = (await db.doc(`billingPurchases/${sha(token)}`).get()).data();
    if (binding && !(await db.doc(`accountDeletions/${binding.uid}`).get()).exists) await verify(binding.uid, token);
  }
  return {handle, verify, notification};
}
module.exports = {createBilling, entitlementFromPurchase, accountId, PACKAGE, PRODUCT, BASE_PLAN,SEAT_PRODUCT};
