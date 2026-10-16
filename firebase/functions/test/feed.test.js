const {test} = require('node:test');
const assert = require('node:assert/strict');
const {createFeedHandler, recipe, photo} = require('../feed');
const {database} = require('./feed-database');
function fixture() {
  const db = database(); let now = 1000000;
  const handler = createFeedHandler(db, () => now);
  return {db, advance: n => { now += n; }, call: (uid, data, moderator = false) => handler({auth: uid ? {uid, token: {feedModerator: moderator}} : null, data})};
}
const content = {action: 'publish', id: 'post-1', caption: 'Dinner!', authorName: 'Chef', recipe: null, photo: ''};
test('feed requires login and derives author ID from verified auth', async () => {
  const f = fixture(); await assert.rejects(f.call(null, {action: 'list'}), {code: 'unauthenticated'});
  await f.call('alice', {...content, authorId: 'someone-else'});
  assert.equal(f.db.docs.get('feedPosts/post-1').authorId, 'alice');
});
test('feed excludes private account data and exposes only public fields', async () => {
  const f = fixture(); f.db.docs.set('users/alice', {email: 'private@example.com', first_name: 'Secret'});
  await f.call('alice', content); const page = await f.call('bob', {action: 'list'});
  assert.equal(page.posts.length, 1); assert.equal(page.posts[0].authorName, 'Chef');
  assert(!JSON.stringify(page).includes('private@example.com')); assert(!('digest' in page.posts[0]));
});
test('publish retry is idempotent but cannot overwrite a post or impersonate its owner', async () => {
  const f = fixture(); await f.call('alice', content); await f.call('alice', content);
  await assert.rejects(f.call('bob', content), {code: 'already-exists'});
  await assert.rejects(f.call('alice', {...content, caption: 'Changed'}), {code: 'already-exists'});
  assert.equal(f.db.docs.get('feedPosts/post-1').caption, 'Dinner!');
});
test('per-account publication rate limit cannot be bypassed by changing post ID', async () => {
  const f = fixture(); await f.call('alice', content);
  await assert.rejects(f.call('alice', {...content, id: 'post-2'}), {code: 'resource-exhausted'});
  f.advance(10000); await f.call('alice', {...content, id: 'post-2'});
});
test('likes are per-account, idempotent and counted atomically', async () => {
  const f = fixture(); await f.call('alice', content);
  await Promise.all(['bob', 'carol', 'bob'].map(uid => f.call(uid, {action: 'like', postId: content.id, liked: true})));
  assert.equal(f.db.docs.get('feedPosts/post-1').likeCount, 2);
  await f.call('bob', {action: 'like', postId: content.id, liked: false}); await f.call('bob', {action: 'like', postId: content.id, liked: false});
  assert.equal(f.db.docs.get('feedPosts/post-1').likeCount, 1);
  assert.equal((await f.call('carol', {action: 'list'})).posts[0].liked, true);
});
test('comment retries and delete retries do not duplicate or corrupt counts', async () => {
  const f = fixture(); await f.call('alice', content);
  const comment = {action: 'comment', postId: content.id, id: 'comment-1', text: 'Lovely!', authorName: 'Bob'};
  await f.call('bob', comment); await f.call('bob', comment);
  assert.equal(f.db.docs.get('feedPosts/post-1').commentCount, 1);
  await assert.rejects(f.call('carol', {action: 'deleteComment', postId: content.id, commentId: 'comment-1'}), {code: 'permission-denied'});
  await f.call('bob', {action: 'deleteComment', postId: content.id, commentId: 'comment-1'}); await f.call('bob', {action: 'deleteComment', postId: content.id, commentId: 'comment-1'});
  assert.equal(f.db.docs.get('feedPosts/post-1').commentCount, 0);
  assert.deepEqual((await f.call('alice', {action: 'comments', postId: content.id})).comments, []);
});
test('comment rate limit is per user across posts', async () => {
  const f = fixture(); await f.call('alice', content); await f.call('carol', {...content, id: 'post-2'});
  await f.call('bob', {action: 'comment', postId: content.id, id: 'c1', text: 'Nice', authorName: 'Bob'});
  await assert.rejects(f.call('bob', {action: 'comment', postId: 'post-2', id: 'c2', text: 'Nice', authorName: 'Bob'}), {code: 'resource-exhausted'});
});
test('reports remain private to moderators and can be marked reviewed', async () => {
  const f = fixture(); await f.call('alice', content);
  await f.call('bob', {action: 'report', postId: content.id, reason: 'Please review'});
  await assert.rejects(f.call('alice', {action: 'moderation'}), {code: 'permission-denied'});
  const page = await f.call('mod', {action: 'moderation'}, true); assert.equal(page.posts[0].reports[0].reason, 'Please review');
  assert(!('reports' in (await f.call('alice', {action: 'list'})).posts[0]));
  await f.call('mod', {action: 'resolveReport', postId: content.id, reporterId: 'bob'}, true);
  assert.deepEqual((await f.call('mod', {action: 'moderation'}, true)).posts[0].reports, []);
});
test('ordinary authors cannot grant themselves moderation privileges', async () => {
  const f = fixture(); await f.call('alice', content);
  await assert.rejects(f.call('alice', {action: 'moderate', postId: content.id, hidden: true, feedModerator: true}), {code: 'permission-denied'});
  await assert.rejects(f.call('bob', {action: 'delete', postId: content.id}), {code: 'permission-denied'});
});
test('hidden/deleted posts and conversations cannot be accessed or modified', async () => {
  const f = fixture(); await f.call('alice', content);
  await f.call('mod', {action: 'moderate', postId: content.id, hidden: true}, true);
  assert.deepEqual((await f.call('alice', {action: 'list'})).posts, []);
  for (const action of ['like','comments','comment','report']) await assert.rejects(f.call('bob', {action, postId: content.id, liked: true}), {code: 'not-found'});
  await f.call('mod', {action: 'moderate', postId: content.id, hidden: false}, true);
  await f.call('alice', {action: 'delete', postId: content.id});
  assert(!('caption' in f.db.docs.get('feedPosts/post-1')));
  await assert.rejects(f.call('mod', {action: 'moderate', postId: content.id, hidden: false}, true), {code: 'invalid-argument'});
});
test('pagination handles equal timestamps without gaps or repeated posts', async () => {
  const f = fixture(); for (let i = 0; i < 32; i++) await f.call(`user${i}`, {...content, id: `post-${String(i).padStart(2, '0')}`});
  const all = []; let cursor = null;
  do { const page = await f.call('reader', {action: 'list', cursor}); all.push(...page.posts.map(p => p.id)); cursor = page.cursor; } while (cursor);
  assert.equal(all.length, 32); assert.equal(new Set(all).size, 32);
});
test('hidden pages still advance the cursor rather than trapping the reader', async () => {
  const f = fixture(); for (let i = 0; i < 16; i++) { await f.call(`user${i}`, {...content, id: `post-${String(i).padStart(2, '0')}`}); if (i) await f.call('mod', {action: 'moderate', postId: `post-${String(i).padStart(2, '0')}`, hidden: true}, true); }
  const first = await f.call('reader', {action: 'list'}); assert.equal(first.posts.length, 0); assert(first.cursor);
  assert.equal((await f.call('reader', {action: 'list', cursor: first.cursor})).posts.length, 1);
});
test('input limits reject oversized content, unsafe IDs and invalid recipe values', async () => {
  const f = fixture();
  await assert.rejects(f.call('alice', {...content, caption: 'x'.repeat(2001)}), {code: 'invalid-argument'});
  await assert.rejects(f.call('alice', {...content, id: '../private'}), {code: 'invalid-argument'});
  assert.throws(() => recipe({title: 'Test', method: '', servings: -1, prepMinutes: 0, ingredients: []}), {code: 'invalid-argument'});
  assert.throws(() => photo('https://example.com/private-image'), {code: 'invalid-argument'});
});
test('photo dimensions prevent tiny compressed files from advertising huge decoded images', () => {
  const png = Buffer.alloc(24); Buffer.from('89504e470d0a1a0a', 'hex').copy(png); png.writeUInt32BE(100000, 16); png.writeUInt32BE(100000, 20);
  assert.throws(() => photo(png.toString('base64')), {code: 'invalid-argument'});
  assert.throws(() => photo(Buffer.from([255,216,255]).toString('base64')), {code: 'invalid-argument'});
});
test('recipe snapshots and server-owned post fields ignore private/injected metadata', async () => {
  const f = fixture();
  await f.call('alice', {...content, createdAt: -1, likeCount: 999, hidden: true, recipe: {title: 'Pasta', method: 'Cook', servings: 2, prepMinutes: 20,
    FirebaseUID: 'private-owner', localId: 42, ingredients: [{name: 'Pasta', quantity: '200 g', category: 'Pantry', note: '', InBasket: true, RecipeId: 42}]}});
  const p = (await f.call('reader', {action: 'list'})).posts[0];
  assert.equal(p.createdAt, 1000000); assert.equal(p.likeCount, 0);
  assert(!JSON.stringify(p.recipe).includes('private-owner')); assert(!JSON.stringify(p.recipe).includes('InBasket')); assert(!JSON.stringify(p.recipe).includes('RecipeId'));
  assert.throws(() => recipe({title: 'Bad', method: '', servings: 2, prepMinutes: 2, ingredients: [null]}), {code: 'invalid-argument'});
});
