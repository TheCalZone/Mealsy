const {fail} = require('./domain');
const {FieldPath} = require('firebase-admin/firestore');
const {createHash} = require('node:crypto');
const hash = value => createHash('sha256').update(JSON.stringify(value)).digest('hex');
function text(value, max, required = false) {
  if (typeof value !== 'string' || value.length > max || (required && !value.trim())) fail('invalid-argument', `Text must be ${required ? 'non-empty and ' : ''}at most ${max} characters.`);
  return value.trim();
}
function id(value) {
  if (typeof value !== 'string' || !/^[a-zA-Z0-9_-]{1,100}$/.test(value)) fail('invalid-argument', 'Invalid identifier.');
  return value;
}
function recipe(value) {
  if (value == null) return null;
  if (!Array.isArray(value.ingredients) || value.ingredients.length > 100 || value.ingredients.some(i => !i || typeof i !== 'object')) fail('invalid-argument', 'Recipes support up to 100 ingredients.');
  if (value.ingredients.some(i => i.isKey != null && typeof i.isKey !== 'boolean')) fail('invalid-argument', 'Invalid key ingredient flag.');
  const integer = n => { if (!Number.isInteger(n) || n < 0 || n > 10000) fail('invalid-argument', 'Invalid serving or preparation time.'); return n; };
  return {description: text(value.description ?? "", 500), cookMinutes: integer(value.cookMinutes ?? 0), title: text(value.title, 160, true), method: text(value.method, 15000), servings: integer(value.servings),
    prepMinutes: integer(value.prepMinutes), ingredients: value.ingredients.map(i => ({name: text(i.name, 200, true), quantity: text(i.quantity, 200), category: text(i.category, 100), note: text(i.note, 500), section: text(i.section ?? "Ingredients", 100), isKey: i.isKey == null ? true : i.isKey}))};
}
function photo(value) {
  if (!value) return '';
  if (typeof value !== 'string' || value.length > 200000 || !/^[A-Za-z0-9+/]+={0,2}$/.test(value)) fail('invalid-argument', 'Use a JPEG/PNG photo smaller than 150 KB.');
  const bytes = Buffer.from(value, 'base64');
  if (bytes.length > 150000 || bytes.toString('base64') !== value) fail('invalid-argument', 'Invalid or oversized photo.');
  let width = 0, height = 0;
  if (bytes.length >= 24 && bytes.subarray(0, 8).toString('hex') === '89504e470d0a1a0a') {
    width = bytes.readUInt32BE(16); height = bytes.readUInt32BE(20);
  } else if (bytes[0] === 255 && bytes[1] === 216) {
    let pos = 2;
    while (pos + 4 < bytes.length) {
      if (bytes[pos++] !== 255) break;
      while (bytes[pos] === 255) pos++;
      const marker = bytes[pos++];
      if (marker === 0xda || marker === 0xd9 || pos + 2 > bytes.length) break;
      const length = bytes.readUInt16BE(pos);
      if (length < 2 || pos + length > bytes.length) break;
      if (marker >= 0xc0 && marker <= 0xcf && ![0xc4, 0xc8, 0xcc].includes(marker) && length >= 8) {
        height = bytes.readUInt16BE(pos + 3); width = bytes.readUInt16BE(pos + 5); break;
      }
      pos += length;
    }
  }
  if (!width || !height || width > 2048 || height > 2048) fail('invalid-argument', 'Use a JPEG/PNG photo at most 2048 pixels wide and high.');
  return value;
}
function cursor(value) {
  if (value == null) return null;
  if (!Number.isSafeInteger(value.createdAt) || value.createdAt < 0) fail('invalid-argument', 'Invalid page cursor.');
  return {createdAt: value.createdAt, id: id(value.id)};
}
function createFeedHandler(db, clock = Date.now) {
  return async request => {
    if (!request.auth) fail('unauthenticated', 'Sign in to explore the food feed.');
    const uid = request.auth.uid, data = request.data || {}, moderator = request.auth.token.feedModerator === true;
    const posts = db.collection('feedPosts');
    if (data.action === 'list') {
      const next = cursor(data.cursor);
      let query = posts.orderBy('createdAt', 'desc').orderBy(FieldPath.documentId(), 'desc');
      if (next) query = query.startAfter(next.createdAt, next.id);
      // Hidden/deleted records are filtered server-side; cursor advances over them too.
      const snapshot = await query.limit(15).get();
      const visible = snapshot.docs.filter(doc => !doc.data().hidden && !doc.data().deleted);
      const likes = await Promise.all(visible.map(doc => doc.ref.collection('likes').doc(uid).get()));
      const result = visible.map((doc, i) => ({...publicPost(doc), liked: likes[i].exists}));
      const last = snapshot.docs.at(-1);
      return {posts: result, cursor: snapshot.size === 15 && last ? {createdAt: last.data().createdAt, id: last.id} : null, moderator};
    }
    if (data.action === 'moderation') {
      if (!moderator) fail('permission-denied', 'Moderator access is required.');
      const next = cursor(data.cursor);
      let query = posts.orderBy('createdAt', 'desc').orderBy(FieldPath.documentId(), 'desc');
      if (next) query = query.startAfter(next.createdAt, next.id);
      const snapshot = await query.limit(15).get();
      const result = await Promise.all(snapshot.docs.map(async doc => {
        const reports = await doc.ref.collection('reports').where('resolved', '==', false).limit(10).get();
        return {...publicPost(doc), hidden: !!doc.data().hidden, deleted: !!doc.data().deleted,
          reports: reports.docs.map(report => ({id: report.id, ...report.data()}))};
      }));
      const last = snapshot.docs.at(-1);
      return {posts: result, cursor: snapshot.size === 15 && last ? {createdAt: last.data().createdAt, id: last.id} : null};
    }
    if (data.action === 'publish') {
      const ref = posts.doc(id(data.id));
      const body = {caption: text(data.caption, 2000), authorName: text(data.authorName, 60, true), recipe: recipe(data.recipe), photo: photo(data.photo)};
      if (!body.caption && !body.recipe) fail('invalid-argument', 'Write a caption or attach a recipe.');
      const digest = hash(body), limitRef = db.doc(`feedLimits/${uid}`);
      return db.runTransaction(async tx => {
        const [existing, limits] = await Promise.all([tx.get(ref), tx.get(limitRef)]);
        if (existing.exists) {
          if (existing.data().authorId === uid && existing.data().digest === digest) return {id: ref.id};
          fail('already-exists', 'That post was already published. Refresh the feed before trying a new post.');
        }
        const now = clock();
        if (limits.exists && now - (limits.data().postAt || 0) < 10000) fail('resource-exhausted', 'Wait a few seconds before publishing again.');
        tx.create(ref, {...body, authorId: uid, createdAt: now, digest, likeCount: 0, commentCount: 0, hidden: false, deleted: false});
        tx.set(limitRef, {...limits.data(), postAt: now});
        return {id: ref.id};
      });
    }
    const ref = posts.doc(id(data.postId));
    if (data.action === 'comments') {
      return db.runTransaction(async tx => {
        const post = (await tx.get(ref)).data(); requireVisible(post);
        const next = cursor(data.cursor);
        let query = ref.collection('comments').orderBy('createdAt', 'desc').orderBy(FieldPath.documentId(), 'desc');
        if (next) query = query.startAfter(next.createdAt, next.id);
        const snapshot = await tx.get(query.limit(20));
        const last = snapshot.docs.at(-1);
        return {comments: snapshot.docs.filter(d => !d.data().deleted).map(d => ({id: d.id, ...d.data()})),
          cursor: snapshot.size === 20 && last ? {createdAt: last.data().createdAt, id: last.id} : null};
      });
    }
    return db.runTransaction(async tx => {
      const post = (await tx.get(ref)).data();
      if (!post) fail('not-found', 'This post no longer exists.');
      if (data.action === 'delete') {
        if (post.authorId !== uid && !moderator) fail('permission-denied', 'Only the author or a moderator can remove this post.');
        // Tombstone removes shared text/photo immediately and keeps orphaned likes/comments inaccessible.
        tx.set(ref, {authorId: post.authorId, createdAt: post.createdAt, deleted: true, hidden: true}); return {};
      }
      if (data.action === 'moderate') {
        if (!moderator) fail('permission-denied', 'Moderator access is required.');
        if (typeof data.hidden !== 'boolean' || post.deleted) fail('invalid-argument', 'Choose an existing post to hide or restore.');
        tx.update(ref, {hidden: data.hidden}); return {};
      }
      if (data.action === 'resolveReport') {
        if (!moderator) fail('permission-denied', 'Moderator access is required.');
        const reportRef = ref.collection('reports').doc(id(data.reporterId));
        const report = await tx.get(reportRef);
        if (!report.exists) fail('not-found', 'Report no longer exists.');
        tx.update(reportRef, {resolved: true}); return {};
      }
      requireVisible(post);
      if (data.action === 'like') {
        if (typeof data.liked !== 'boolean') fail('invalid-argument', 'Choose a like state.');
        const likeRef = ref.collection('likes').doc(uid), like = await tx.get(likeRef);
        if (data.liked !== like.exists) {
          if (data.liked) tx.create(likeRef, {createdAt: clock()}); else tx.delete(likeRef);
          tx.update(ref, {likeCount: Math.max(0, (post.likeCount || 0) + (data.liked ? 1 : -1))});
        }
        return {};
      }
      if (data.action === 'comment') {
        const commentRef = ref.collection('comments').doc(id(data.id));
        const message = text(data.text, 1000, true), authorName = text(data.authorName, 60, true);
        const limitRef = db.doc(`feedLimits/${uid}`);
        const [existing, limits] = await Promise.all([tx.get(commentRef), tx.get(limitRef)]);
        if (existing.exists) {
          if (existing.data().authorId === uid && existing.data().text === message && !existing.data().deleted) return {};
          fail('already-exists', 'This comment was already submitted.');
        }
        const now = clock();
        if (limits.exists && now - (limits.data().commentAt || 0) < 3000) fail('resource-exhausted', 'Wait a moment before commenting again.');
        tx.create(commentRef, {text: message, authorName, authorId: uid, createdAt: now, deleted: false});
        tx.update(ref, {commentCount: (post.commentCount || 0) + 1});
        tx.set(limitRef, {...limits.data(), commentAt: now}); return {};
      }
      if (data.action === 'deleteComment') {
        const commentRef = ref.collection('comments').doc(id(data.commentId)), comment = (await tx.get(commentRef)).data();
        if (!comment) fail('not-found', 'This comment no longer exists.');
        if (comment.authorId !== uid && !moderator) fail('permission-denied', 'Only the author or a moderator can remove this comment.');
        if (!comment.deleted) {
          tx.set(commentRef, {authorId: comment.authorId, createdAt: comment.createdAt, deleted: true});
          tx.update(ref, {commentCount: Math.max(0, (post.commentCount || 0) - 1)});
        }
        return {};
      }
      if (data.action === 'report') {
        const reason = text(data.reason, 500, true);
        tx.set(ref.collection('reports').doc(uid), {reason, reporterId: uid, createdAt: clock(), resolved: false}); return {};
      }
      fail('invalid-argument', 'Unknown feed action.');
    });
  };
}
function requireVisible(post) { if (!post || post.hidden || post.deleted) fail('not-found', 'This post is no longer available.'); }
function publicPost(doc) {
  const p = doc.data();
  return {id: doc.id, caption: p.caption, authorId: p.authorId, authorName: p.authorName, createdAt: p.createdAt,
    recipe: p.recipe, photo: p.photo, likeCount: p.likeCount, commentCount: p.commentCount};
}
module.exports = {createFeedHandler, recipe, photo, cursor};
