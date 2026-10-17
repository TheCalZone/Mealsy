const {test} = require('node:test');
const assert = require('node:assert/strict');
const {database} = require('./feed-database');
const {createFeedHandler} = require('../feed');
const {createBilling} = require('../billing');
const {createHandler} = require('../application');
function fixture() {
  const db = database(), feed = createFeedHandler(db, () => 1000000, async () => ({status: 'approved', provider: 'test'}));
  const call = (uid, data) => {
    db.docs.set(`communityProfiles/${uid}`, {username: uid, discriminator: '1001'});
    return feed({auth: {uid, token: {email_verified: true}}, data});
  };
  const publish = (uid, id, caption) => call(uid, {action: 'publish', id, caption, authorName: uid, photo: '', recipe: null});
  return {db, call, publish};
}
test('interests persist privately, reject invented topics, and complete onboarding once', async () => {
  const f = fixture(); assert.equal((await f.call('reader', {action: 'preferences'})).setupComplete, false);
  await f.call('reader', {action: 'savePreferences', interests: ['Italian', 'Italian']});
  const saved = await f.call('reader', {action: 'preferences'});
  assert.deepEqual(saved.interests, ['Italian']); assert(saved.setupComplete);
  assert.deepEqual((await f.call('other', {action: 'preferences', uid: 'reader'})).interests, []);
  await assert.rejects(f.call('reader', {action: 'savePreferences', interests: ['not-a-topic']}), {code: 'invalid-argument'});
  assert.deepEqual((await f.call('reader', {action: 'preferences'})).interests, ['Italian']);
});
test('following is idempotent, account scoped and can be removed after a post is deleted', async () => {
  const f = fixture(); await f.publish('chef', 'pasta', 'Pasta');
  for (let i=0;i<2;i++) await f.call('reader', {action: 'follow', postId: 'pasta', following: true, authorId: 'someone-else'});
  assert.equal((await f.call('reader', {action: 'preferences'})).following.length, 1);
  const page = await f.call('reader', {action: 'list', mode: 'following'});
  assert.equal(page.posts[0].following, true); assert.equal(page.posts[0].authorId, 'chef');
  assert.deepEqual((await f.call('other', {action: 'list', mode: 'following'})).posts, []);
  await assert.rejects(f.call('chef', {action: 'follow', postId: 'pasta', following: true}), {code: 'invalid-argument'});
  await f.call('chef', {action: 'delete', postId: 'pasta'});
  await f.call('reader', {action: 'unfollow', authorId: 'chef'});
  assert.deepEqual((await f.call('reader', {action: 'preferences'})).following, []);
});
test('For you boosts explicit interests and learns from idempotent likes and unlikes', async () => {
  const f = fixture(); await f.publish('cook1', 'a-pasta', 'Italian pasta'); await f.publish('cook2', 'z-cake', 'Chocolate cake');
  await f.call('reader', {action: 'savePreferences', interests: ['Italian']});
  assert.equal((await f.call('reader', {action: 'list', mode: 'forYou'})).posts[0].id, 'a-pasta');
  await f.call('reader', {action: 'savePreferences', interests: []});
  await f.call('reader', {action: 'like', postId: 'a-pasta', liked: true});
  await f.call('reader', {action: 'like', postId: 'a-pasta', liked: true});
  assert.equal(f.db.docs.get('feedAccounts/reader').affinities.Italian, 1);
  assert.equal((await f.call('reader', {action: 'list', mode: 'forYou'})).posts[0].id, 'a-pasta');
  await f.call('reader', {action: 'like', postId: 'a-pasta', liked: false});
  assert.equal(f.db.docs.get('feedAccounts/reader').affinities.Italian, 0);
});
test('search matches related public words, excludes hidden posts and advances empty pages', async () => {
  const f = fixture(); await f.publish('oldcook', 'a-match', 'Crème fraîche pasta');
  for (let i=0;i<30;i++) await f.publish(`cook${i}`, `z-${i}`, 'Cake');
  const first = await f.call('reader', {action: 'list', search: 'creme pasta'});
  assert.equal(first.posts.length, 0); assert(first.cursor);
  const older = await f.call('reader', {action: 'list', search: 'creme pasta', cursor: first.cursor});
  assert.equal(older.posts[0].id, 'a-match');
  assert.equal((await f.call('reader', {action: 'list', search: 'Italian', cursor: first.cursor})).posts[0].id, 'a-match');
  f.db.docs.get('feedPosts/a-match').hidden = true;
  assert.equal((await f.call('reader', {action: 'list', search: 'pasta', cursor: first.cursor})).posts.length, 0);
  await assert.rejects(f.call('reader', {action: 'list', search: 'x'.repeat(121)}), {code: 'invalid-argument'});
});
test('follow limits preserve existing follows and hidden posts cannot be used to follow', async () => {
  const f = fixture(); await f.publish('chef', 'pasta', 'Pasta');
  f.db.docs.set('feedAccounts/reader', {following: Object.fromEntries(Array.from({length: 200}, (_,i) => [`cook${i}`, 'Cook']))});
  await assert.rejects(f.call('reader', {action: 'follow', postId: 'pasta', following: true}), {code: 'resource-exhausted'});
  f.db.docs.get('feedPosts/pasta').hidden = true;
  await assert.rejects(f.call('other', {action: 'follow', postId: 'pasta', following: true}), {code: 'not-found'});
});
test('admin Family grants unlock owner and members, expire and revoke without Play credentials', async () => {
  const db = database(), billing = createBilling(db), household = createHandler(db);
  const request = (uid, data) => ({auth: {uid, token: {}}, data});
  db.docs.set('users/owner', {active: true, expiresAt: Date.now()+86400000});
  assert.equal((await billing.handle(request('owner', {action: 'status'}))).ownPlan, false);
  db.docs.set('familyGrants/owner', {active: true, expiresAt: Date.now()+86400000});
  const plan = await billing.handle(request('owner', {action: 'status'})); assert(plan.ownPlan); assert.equal(plan.basePlanId, 'admin');
  await household(request('owner', {action: 'create', name: 'Test family'}));
  const {code} = await household(request('owner', {action: 'invite'}));
  await household(request('member', {action: 'join', code}));
  assert((await billing.handle(request('member', {action: 'status'}))).familyEnabled);
  db.docs.get('familyGrants/owner').expiresAt = Date.now()-1;
  assert.equal((await billing.handle(request('member', {action: 'status'}))).familyEnabled, false);
  await assert.rejects(household(request('owner', {action: 'invite'})), {code: 'permission-denied'});
  db.docs.delete('familyGrants/owner');
  assert.equal((await household(request('owner', {action: 'status'}))).familyEnabled, false);
});
