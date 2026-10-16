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
  await db.doc('billingEntitlements/owner').set({active: true, expiresAt: Date.now() + 86400000});
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
test('feed Firestore transactions count likes once and enforce hidden-post access', async () => {
  const feed = require('./functions/feed').createFeedHandler(db);
  const request = (uid, data, moderator = false) => feed({auth: {uid, token: {feedModerator: moderator}}, data});
  await request('feed-owner', {action: 'publish', id: 'real-feed-post', caption: 'A real emulator meal', authorName: 'Cook', recipe: null, photo: ''});
  await Promise.all(Array.from({length: 4}, () => request('reader', {action: 'like', postId: 'real-feed-post', liked: true})));
  const listed = await request('reader', {action: 'list'});
  assert.equal(listed.posts[0].likeCount, 1); assert.equal(listed.posts[0].liked, true);
  await request('reader', {action: 'comment', postId: 'real-feed-post', id: 'real-comment', text: 'Looks delicious', authorName: 'Reader'});
  assert.equal((await request('reader', {action: 'comments', postId: 'real-feed-post'})).comments.length, 1);
  await request('mod', {action: 'moderate', postId: 'real-feed-post', hidden: true}, true);
  assert.equal((await request('reader', {action: 'list'})).posts.length, 0);
  await assert.rejects(request('reader', {action: 'comments', postId: 'real-feed-post'}), {code: 'not-found'});
  const signedIn = rules.authenticatedContext('feed-owner').firestore();
  for (const path of ['feedPosts/real-feed-post', 'feedPosts/real-feed-post/likes/reader', 'feedPosts/real-feed-post/comments/real-comment', 'feedPosts/real-feed-post/reports/reader', 'feedLimits/feed-owner']) {
    await assertFails(getDoc(doc(signedIn, path))); await assertFails(setDoc(doc(signedIn, path), {authorId: 'feed-owner'}));
  }
});
