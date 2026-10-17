const {test} = require('node:test');
const assert = require('node:assert/strict');
const {createHandler} = require('../application');
const {createHash} = require('node:crypto');
// A serial transaction test double. Staged writes commit only when the handler
// succeeds; reads after writes are rejected just as they are by Firestore.
// Real Firestore/rules integration is covered separately by the emulator suite.
function database() {
  const docs = new Map(); let counter = 0; let queue = Promise.resolve();
  const ref = path => ({path, id: path.split('/').at(-1), collection: name => collection(`${path}/${name}`)});
  const collection = path => ({doc: (id = `generated-${++counter}`) => ref(`${path}/${id}`)});
  return {docs, doc: ref, collection, runTransaction(action) {
    const run = queue.then(async () => {
      const staged = new Map();
      const tx = {
        get: async r => { assert.equal(staged.size, 0, 'Firestore requires all reads before writes'); const data = structuredClone(docs.get(r.path)); return {exists: data !== undefined, data: () => data}; },
        set: (r, value) => staged.set(r.path, structuredClone(value)),
        create: (r, value) => { assert(!docs.has(r.path)); staged.set(r.path, structuredClone(value)); },
        update: (r, value) => { assert(docs.has(r.path)); staged.set(r.path, {...docs.get(r.path), ...structuredClone(value)}); },
        delete: r => staged.set(r.path, undefined)
      };
      const result = await action(tx);
      for (const [path, value] of staged) value === undefined ? docs.delete(path) : docs.set(path, value);
      return result;
    });
    queue = run.catch(() => {}); return run;
  }};
}
function fixture() {
  const db = database();
  for (const uid of ["parent", "stranger"]) db.docs.set(`billingEntitlements/${uid}`, {active: true, expiresAt: Date.now() + 86400000});
  const handler = createHandler(db);
  const call = (uid, data) => handler({auth: uid ? {uid, token: {email: `${uid}@example.com`}} : null, data});
  return {db, call};
}
const milk = {Id: '11111111-1111-1111-1111-111111111111', Name: 'Milk', Category: 'Dairy', Quantity: '', Note: '', InBasket: false};
async function family(f) {
  const {householdId} = await f.call('parent', {action: 'create', name: 'Our family'});
  const {code} = await f.call('parent', {action: 'invite'});
  await f.call('child', {action: 'join', code}); return householdId;
}
test('all actions require authentication', async () => {
  const f = fixture(); const before = f.db.docs.size; await assert.rejects(f.call(null, {action: 'status'}), {code: 'unauthenticated'}); assert.equal(f.db.docs.size, before);
});
test('create/join return the same household with owner and members', async () => {
  const f = fixture(); const id = await family(f);
  const status = await f.call('child', {action: 'status'});
  assert.equal(status.householdId, id); assert.equal(status.owner, 'parent'); assert.equal(status.members.length, 2);
  await assert.rejects(f.call('child', {action: 'create', name: 'Other'}), {code: 'failed-precondition'});
});
test('invitations are hashed, expiring, single use and atomically accepted', async () => {
  const f = fixture(); await f.call('parent', {action: 'create', name: 'Family'});
  const {code} = await f.call('parent', {action: 'invite'});
  assert.match(code, /^[a-f0-9]{48}$/);
  assert(![...f.db.docs.keys()].some(key => key.includes(code)));
  const results = await Promise.allSettled(['alice', 'bob'].map(uid => f.call(uid, {action: 'join', code})));
  assert.equal(results.filter(r => r.status === 'fulfilled').length, 1);
  assert.equal(results.filter(r => r.status === 'rejected' && r.reason.code === 'not-found').length, 1);
});
test('expired invitations cannot grant access', async () => {
  const f = fixture(); await f.call('parent', {action: 'create', name: 'Family'});
  const {code} = await f.call('parent', {action: 'invite'});
  const path = `householdInvites/${createHash('sha256').update(code).digest('hex')}`;
  f.db.docs.get(path).expires = Date.now() - 1;
  await assert.rejects(f.call('child', {action: 'join', code}), {code: 'not-found'});
  assert(!f.db.docs.has('householdAccounts/child'));
});
test('new invitation revokes previous code; explicit revocation works', async () => {
  const f = fixture(); await f.call('parent', {action: 'create', name: 'Family'});
  const first = await f.call('parent', {action: 'invite'});
  const second = await f.call('parent', {action: 'invite'});
  await assert.rejects(f.call('child', {action: 'join', code: first.code}), {code: 'not-found'});
  await f.call('parent', {action: 'revoke'});
  await assert.rejects(f.call('child', {action: 'join', code: second.code}), {code: 'not-found'});
});
test('member can edit shared list but cannot invite or remove family', async () => {
  const f = fixture(); const householdId = await family(f);
  await f.call('child', {action: 'patch', scope: 'household', householdId, changes: [{before: null, after: milk}]});
  assert.equal((await f.call('parent', {action: 'read', scope: 'household', householdId})).items[0].Name, 'Milk');
  await assert.rejects(f.call('child', {action: 'invite'}), {code: 'permission-denied'});
  await assert.rejects(f.call('child', {action: 'remove', member: 'parent'}), {code: 'permission-denied'});
});
test('non-members cannot read or edit the household list', async () => {
  const f = fixture(); const householdId = await family(f);
  for (const action of ['read', 'patch']) await assert.rejects(f.call('stranger', {action, scope: 'household', householdId, changes: []}), {code: 'failed-precondition'});
  await f.call('stranger', {action: 'create', name: 'Other family'});
  await assert.rejects(f.call('stranger', {action: 'read', scope: 'household', householdId}), {code: 'permission-denied'});
});
test('removing a member immediately prevents subsequent shared writes', async () => {
  const f = fixture(); const householdId = await family(f);
  await f.call('parent', {action: 'remove', member: 'child'});
  await assert.rejects(f.call('child', {action: 'patch', scope: 'household', householdId, changes: [{after: milk}]}), {code: 'failed-precondition'});
  assert.equal((await f.call('parent', {action: 'status'})).members.length, 1);
});
test('leave preserves personal data and owners cannot leave themselves', async () => {
  const f = fixture(); await family(f);
  await f.call('child', {action: 'patch', scope: 'personal', changes: [{after: milk}]});
  await f.call('child', {action: 'leave'});
  assert.equal((await f.call('child', {action: 'status'})).householdId, null);
  assert.equal((await f.call('child', {action: 'read', scope: 'personal'})).items.length, 1);
  await assert.rejects(f.call('parent', {action: 'leave'}), {code: 'failed-precondition'});
});
test('personal lists ignore attempted UID spoofing and remain private between members', async () => {
  const f = fixture(); await family(f);
  await f.call('parent', {action: 'patch', scope: 'personal', changes: [{after: milk}]});
  const child = await f.call('child', {action: 'read', scope: 'personal', uid: 'parent'});
  assert.deepEqual(child.items, []);
});
test('failed conflicting patch is atomic, including its earlier additions', async () => {
  const f = fixture();
  await f.call('parent', {action: 'patch', scope: 'personal', changes: [{after: {...milk, Quantity: '2 L'}}]});
  const rice = {...milk, Id: '22222222-2222-2222-2222-222222222222', Name: 'Rice'};
  await assert.rejects(f.call('parent', {action: 'patch', scope: 'personal', changes: [{after: rice}, {before: milk, after: {...milk, Quantity: '3 L'}}]}), {code: 'aborted'});
  const result = await f.call('parent', {action: 'read', scope: 'personal'});
  assert.equal(result.items.length, 1); assert.equal(result.items[0].Quantity, '2 L');
});
test('household size limit prevents invite acceptance without consuming the code', async () => {
  const f = fixture(); const {householdId} = await f.call('parent', {action: 'create', name: 'Full'});
  const {code} = await f.call('parent', {action: 'invite'});
  const house = f.db.docs.get(`households/${householdId}`);
  for (let i = 0; i < 3; i++) house.members[`member${i}`] = 'Member';
  await assert.rejects(f.call('child', {action: 'join', code}), {code: 'resource-exhausted'});
  assert.equal([...f.db.docs.keys()].filter(k => k.startsWith('householdInvites/')).length, 1);
});
test('owner can delete household without deleting private lists; members cannot', async () => {
  const f = fixture(); const householdId = await family(f);
  const {code} = await f.call('parent', {action: 'invite'});
  await f.call('child', {action: 'patch', scope: 'personal', changes: [{after: milk}]});
  await f.call('child', {action: 'patch', scope: 'household', householdId, changes: [{after: milk}]});
  await assert.rejects(f.call('child', {action: 'delete'}), {code: 'permission-denied'});
  await f.call('parent', {action: 'delete'});
  assert.equal((await f.call('child', {action: 'status'})).householdId, null);
  assert.equal((await f.call('child', {action: 'read', scope: 'personal'})).items.length, 1);
  assert(!f.db.docs.has(`households/${householdId}/lists/shopping`));
  await assert.rejects(f.call('newMember', {action: 'join', code}), {code: 'not-found'});
  await f.call('parent', {action: 'create', name: 'New household'});
});
