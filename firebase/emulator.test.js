// Run only through firebase emulators:exec, using a demo project (never production).
const {test, before, after} = require('node:test');
const assert = require('node:assert/strict');
const {readFileSync} = require('node:fs');
const {createRequire} = require('node:module');
const localRequire = createRequire(`${__dirname}/functions/package.json`);
const {initializeTestEnvironment, assertFails, assertSucceeds} = localRequire('@firebase/rules-unit-testing');
const {doc, getDoc, setDoc} = localRequire('firebase/firestore');
const {initializeApp, deleteApp} = localRequire('firebase-admin/app');
const {getFirestore} = localRequire('firebase-admin/firestore');
const {createHandler} = require('./functions/application');
let rules, app, db, handle;
before(async () => {
  assert(process.env.FIRESTORE_EMULATOR_HOST, 'Start Firestore emulator first; production is prohibited');
  rules = await initializeTestEnvironment({projectId: 'demo-mealsy', firestore: {rules: readFileSync(`${__dirname}/firestore.rules`, 'utf8')}});
  await rules.clearFirestore();
  app = initializeApp({projectId: 'demo-mealsy'}, 'household-tests'); db = getFirestore(app); handle = createHandler(db);
});
after(async () => { if (rules) await rules.cleanup(); if (app) await deleteApp(app); });
const call = (uid, data) => handle({auth: uid ? {uid, token: {email: `${uid}@example.com`}} : null, data});
test('rules preserve private recipes and deny direct access to household/server documents', async () => {
  const parent = rules.authenticatedContext('parent').firestore();
  const child = rules.authenticatedContext('child').firestore();
  const guest = rules.unauthenticatedContext().firestore();
  await assertSucceeds(setDoc(doc(parent, 'users/parent/recipes/1'), {Name: 'Private recipe'}));
  await assertFails(getDoc(doc(child, 'users/parent/recipes/1')));
  await assertFails(getDoc(doc(guest, 'users/parent/recipes/1')));
  for (const path of ['households/fake', 'householdAccounts/parent', 'householdInvites/fake', 'shoppingLists/parent', 'households/fake/lists/shopping']) {
    await assertFails(setDoc(doc(parent, path), {owner: 'parent'}));
    await assertFails(getDoc(doc(parent, path)));
  }
});
test('real transactions enforce single-use acceptance, shared editing, privacy and revocation', async () => {
  const {householdId} = await call('owner', {action: 'create', name: 'Test family'});
  const {code} = await call('owner', {action: 'invite'});
  const joins = await Promise.allSettled(['alice', 'bob'].map(uid => call(uid, {action: 'join', code})));
  assert.equal(joins.filter(r => r.status === 'fulfilled').length, 1);
  const member = joins[0].status === 'fulfilled' ? 'alice' : 'bob';
  const item = {Id: '11111111-1111-1111-1111-111111111111', Name: 'Milk', Category: 'Dairy', Quantity: '', Note: '', InBasket: false};
  await call(member, {action: 'patch', scope: 'household', householdId, changes: [{after: item}]});
  assert.equal((await call('owner', {action: 'read', scope: 'household', householdId})).items.length, 1);
  await call('owner', {action: 'patch', scope: 'personal', changes: [{after: item}]});
  assert.deepEqual((await call(member, {action: 'read', scope: 'personal', uid: 'owner'})).items, []);
  await call('owner', {action: 'remove', member});
  await assert.rejects(call(member, {action: 'read', scope: 'household', householdId}), {code: 'failed-precondition'});
});
