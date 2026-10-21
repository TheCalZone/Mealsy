const {fail} = require('./domain');
const {requestData} = require('./request');
const discovery = require('./feed-discovery');
const community = require('./community');
const {requireRole}=require('./business-access');
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
  if (!Number.isInteger(value.servings) || value.servings < 1) fail('invalid-argument', 'Servings must be at least one.');
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
function createFeedHandler(db, clock = Date.now, moderate = require('./moderation').moderate) {
  return async request => {
    if (!request.auth) fail('unauthenticated', 'Sign in to explore the food feed.');
    const uid = request.auth.uid, data = requestData(request, ['getPost', 'profile', 'setUsername', 'requestPromotion', 'activatePromotion', 'preferences', 'savePreferences', 'follow', 'unfollow', 'list', 'moderation', 'publish', 'comments', 'delete', 'moderate', 'resolveReport', 'like', 'comment', 'deleteComment', 'report']), moderator = request.auth.token?.feedModerator === true;
    if (data.action === 'profile') return {...(await db.doc(`communityProfiles/${uid}`).get()).data(), emailVerified: request.auth.token?.email_verified === true, promotionCheckoutEnabled: false};
    if (data.action === 'setUsername') return community.setUsername(db, uid, data.username, moderate);
    const accountRef = db.doc(`feedAccounts/${uid}`);
    if (data.action === 'preferences') {
      const account = (await accountRef.get()).data() || {};
      return {interests: account.interests || [], setupComplete: account.setupComplete === true,
        excludedIngredients:account.excludedIngredients||[],
        options: Object.keys(discovery.topics), following: Object.entries(account.following || {}).map(([id, name]) => ({id, name}))};
    }
    if (data.action === 'savePreferences') {
      const interests = discovery.interests(data.interests);
      const excluded=data.excludedIngredients;
      if(excluded!==undefined&&(!Array.isArray(excluded)||excluded.length>50||excluded.some(s=>typeof s!=='string'||!s.trim()||s.length>80)))fail('invalid-argument','Use up to 50 ingredient exclusions, each up to 80 characters.');
      return db.runTransaction(async tx => {
        const account = (await tx.get(accountRef)).data() || {};
        tx.set(accountRef, {...account, interests, ...(excluded===undefined?{}:{excludedIngredients:[...new Set(excluded.map(s=>s.trim()))]}), setupComplete: true}); return {};
      });
    }
    if (data.action === 'follow' || data.action === 'unfollow') {
      if (data.action === 'follow' && typeof data.following !== 'boolean') fail('invalid-argument', 'Choose a follow state.');
      const targetId = data.action === 'unfollow' ? id(data.authorId) : null;
      const postRef = data.action === 'follow' ? db.doc(`feedPosts/${id(data.postId)}`) : null;
      return db.runTransaction(async tx => {
        const account = (await tx.get(accountRef)).data() || {};
        const post = postRef ? (await tx.get(postRef)).data() : null;
        if (postRef) requireVisible(post);
        const author = postRef ? post.authorId : targetId;
        if (author === uid) fail('invalid-argument', 'You cannot follow yourself.');
        const following = {...account.following};
        if (postRef && data.following) {
          if (!Object.hasOwn(following, author) && Object.keys(following).length >= 200) fail('resource-exhausted', 'You can follow up to 200 cooks.');
          if(!Object.hasOwn(following,author))require('./notifications').notify(tx,db,author,`follow:${uid}:${author}:${clock()}`,{type:'follow',postId:postRef.id,message:'Someone started following your recipes.'},clock());
          following[author] = post.authorName;
        } else delete following[author];
        tx.set(accountRef, {...account, following}); return {};
      });
    }
    const posts = db.collection('feedPosts');
    if (data.action === 'getPost') {
      const ref=posts.doc(id(data.postId));
      return db.runTransaction(async tx=>{
        const doc=await tx.get(ref),post=doc.data();requireVisible(post);
        // This legacy namespace serves public posts only. Never use it as a
        // fallback when a privacy-aware social read fails.
        if((post.audience!=null&&post.audience!=='public')||post.privacyManaged===true)fail('not-found','This post is no longer available.');
        const liked=await tx.get(ref.collection('likes').doc(uid));
        return {post:{...publicPost(doc),audience:'public',privacyManaged:false,liked:liked.exists}};
      },{readOnly:true});
    }
    if (data.action === 'list') {
      const terms = discovery.searchTerms(data.search);
      const mode = terms.length ? 'search' : data.mode ?? 'latest';
      if (!['latest', 'following', 'forYou', 'search'].includes(mode)) fail('invalid-argument', 'Choose a feed.');
      const pageSize = mode === 'latest' && terms.length === 0 ? 15 : 30;
      const next = cursor(data.cursor);
      let query = posts.orderBy('createdAt', 'desc').orderBy(FieldPath.documentId(), 'desc');
      if (next) query = query.startAfter(next.createdAt, next.id);
      // Hidden/deleted records are filtered server-side; cursor advances over them too.
      // Read visibility and like state in one consistent snapshot. A concurrent
      // hide/delete cannot leave old post content mixed with newer like reads.
      return db.runTransaction(async tx => {
        const account = (await tx.get(accountRef)).data() || {};
        const snapshot = await tx.get(query.limit(pageSize));
        const businessFollows=await Promise.all(snapshot.docs.map(doc=>doc.data().businessId?tx.get(db.doc(`businessFollows/${createHash('sha256').update(uid+'|'+doc.data().businessId).digest('hex')}`)):null));
        const visible = snapshot.docs.filter((doc,index) => {
          const post = doc.data();
          return !post.hidden && !post.deleted && (mode !== 'following' || Object.hasOwn(account.following || {}, post.authorId)||businessFollows[index]?.exists) &&
            !(account.excludedIngredients||[]).some(name=>(post.recipe?.ingredients||[]).some(i=>discovery.ingredientMatches(i.name,name)))&&
            discovery.matches(post, terms);
        });
        const likes = await Promise.all(visible.map(doc => tx.get(doc.ref.collection('likes').doc(uid))));
        const campaigns = await Promise.all(visible.map(doc => tx.get(db.doc(`promotionCampaigns/${doc.id}`))));
        const receipts = await Promise.all(campaigns.map(c => c.data()?.receiptId ? tx.get(db.doc(`promotionReceipts/${c.data().receiptId}`)) : null));
        const brands=await Promise.all(visible.map(doc=>doc.data().businessId?tx.get(db.doc(`businesses/${id(doc.data().businessId)}`)):null));
        const paid=await Promise.all(campaigns.map(c=>c.data()?.businessCampaignId?tx.get(db.doc(`businessCampaigns/${id(c.data().businessCampaignId)}`)):null));
        const wallets=await Promise.all(paid.map(c=>c?.data()?.businessId?tx.get(db.doc(`businessWallets/${c.data().businessId}`)):null));
        const daily=await Promise.all(paid.map(c=>c?tx.get(c.ref.collection('days').doc(new Date(clock()).toISOString().slice(0,10))):null));
        const commercial=(await tx.get(db.doc('commercialConfig/campaigns'))).data();
        const result = visible.map((doc, i) => ({...publicPost(doc), liked: likes[i].exists,
          businessVerified:brands[i]?.data()?.verified===true,
          promotionCampaignId:paid[i]?.id||null,
          promoted: (paid[i]?commercial?.enabled===true&&campaigns[i].data()?.active===true&&require('./campaign-ledger').eligible(paid[i].data(),clock())&&(wallets[i]?.data()?.balanceMicro||0)>=1000&&(daily[i]?.data()?.spentMicro||0)+1000<=paid[i].data().dailyBudgetMinor*10000:community.promoted(campaigns[i].data(), receipts[i]?.data(), doc.data(), clock()))&&(!doc.data().businessId||brands[i]?.data()?.verified===true)&&(!(account.interests||[]).length||discovery.postTopics(doc.data()).some(t=>account.interests.includes(t))),
          following: Object.hasOwn(account.following || {}, doc.data().authorId)||businessFollows[snapshot.docs.findIndex(d=>d.id===doc.id)]?.exists===true, topics: discovery.postTopics(doc.data()),
          recommendationScore: discovery.score(doc.data(), account)}));
        if (mode === 'forYou' || mode === 'search') result.sort((a, b) => b.recommendationScore - a.recommendationScore || b.createdAt - a.createdAt || b.id.localeCompare(a.id));
        if (mode === 'forYou' || mode === 'search') {
          const sponsored = result.findIndex(p => p.promoted);
          if (sponsored >= 0) result.unshift(result.splice(sponsored, 1)[0]);
        }
        let sponsoredSeen=false;
        // Keep the cursor chronological; cap paid exposure within this page.
        const capped=result.filter(p=>{if(!p.promoted)return true;if(sponsoredSeen)return false;sponsoredSeen=true;return true;});
        const last = snapshot.docs.at(-1);
        return {posts: capped, cursor: snapshot.size === pageSize && last ? {createdAt: last.data().createdAt, id: last.id} : null, moderator};
      }, {readOnly: true});
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
      const body = {caption: text(data.caption, 2000), authorName: '', recipe: recipe(data.recipe), photo: photo(data.photo)};
      if (!body.caption && !body.recipe) fail('invalid-argument', 'Write a caption or attach a recipe.');
      if (Buffer.byteLength(JSON.stringify(body), 'utf8') > 500000) fail('invalid-argument', 'This post is too large. Shorten the recipe or choose a smaller photo.');
      community.verified(request);
      let businessRef;
      if(data.businessId){if(request.businessPublishingAuthorized!==true)fail('permission-denied','Publish business recipes through the business workspace.');businessRef=db.doc(`businesses/${id(data.businessId)}`);const business=requireRole((await businessRef.get()).data(),uid,'editor',true);body.authorName=business.name;body.businessId=businessRef.id;}
      else body.authorName = (await community.identity(db, uid)).username;
      if (Buffer.byteLength(JSON.stringify(body), 'utf8') > 500000) fail('invalid-argument', 'This post is too large. Shorten the recipe or choose a smaller photo.');
      const digest = hash(body), limitRef = db.doc(`feedLimits/${uid}`);
      const previous = (await ref.get()).data();
      if (previous?.authorId === uid && previous.digest === digest) return {id: ref.id};
      if (previous) fail('already-exists', 'That post was already published. Refresh the feed before trying a new post.');
      const recent = (await limitRef.get()).data();
      if (recent && clock() - (recent.postAt || 0) < 10000) fail('resource-exhausted', 'Wait a few seconds before publishing again.');
      const checked = await moderate(body);
      return db.runTransaction(async tx => {
        const [existing, limits] = await Promise.all([tx.get(ref), tx.get(limitRef)]);
        if(businessRef)requireRole((await tx.get(businessRef)).data(),uid,'editor',true);
        if (existing.exists) {
          if (existing.data().authorId === uid && existing.data().digest === digest) return {id: ref.id};
          fail('already-exists', 'That post was already published. Refresh the feed before trying a new post.');
        }
        if(businessRef&&(await tx.get(posts.where('businessId','==',businessRef.id).limit(100))).size>=100)fail('resource-exhausted','This business supports 100 published recipe copies.');
        const now = clock();
        if (limits.exists && now - (limits.data().postAt || 0) < 10000) fail('resource-exhausted', 'Wait a few seconds before publishing again.');
        tx.create(ref, {...body, moderation: {...checked, checkedAt: now}, authorId: uid, createdAt: now, digest, likeCount: 0, commentCount: 0, hidden: false, deleted: false});
        tx.set(limitRef, {...limits.data(), postAt: now});
        return {id: ref.id};
      });
    }
    const ref = posts.doc(id(data.postId));
    if (data.action === 'requestPromotion') {
      community.verified(request);
      return db.runTransaction(async tx => {
        const post = (await tx.get(ref)).data(); requireVisible(post);
        if (post.authorId !== uid || !post.recipe || post.moderation?.status !== 'approved') fail('permission-denied', 'Only your safety-approved recipes can be promoted.');
        tx.set(db.doc(`promotionRequests/${ref.id}`), {owner: uid, postId: ref.id, status: 'awaiting-payment-integration', requestedAt: clock()});
        return {checkoutEnabled: false, status: 'awaiting-payment-integration'};
      });
    }
    if (data.action === 'activatePromotion') {
      if (request.auth.token?.promotionAdmin !== true) fail('permission-denied', 'Promotion administrator access is required.');
      const receiptId = id(data.receiptId);
      return db.runTransaction(async tx => {
        const post = (await tx.get(ref)).data(); requireVisible(post);
        const receipt = (await tx.get(db.doc(`promotionReceipts/${receiptId}`))).data();
        if(post.businessId)fail('failed-precondition','Business campaign checkout must be integrated before paid activation.');
        if (!post.recipe || post.moderation?.status !== 'approved' || receipt?.verified !== true || receipt.revoked === true || receipt.owner !== post.authorId || receipt.postId !== ref.id || !Number.isSafeInteger(receipt.expiresAt) || receipt.expiresAt <= clock()) fail('failed-precondition', 'A verified, unexpired payment for this approved recipe is required.');
        tx.set(db.doc(`promotionCampaigns/${ref.id}`), {active: true, owner: post.authorId, postId: ref.id, receiptId, startsAt: clock(), expiresAt: receipt.expiresAt}); return {};
      });
    }
    let checkedComment, commentAuthor;
    const parentCommentId=data.action==='comment'&&data.parentCommentId?id(data.parentCommentId):null;
    if (data.action === 'comment') {
      requireVisible((await ref.get()).data());
      const message = text(data.text, 1000, true);
      commentAuthor = (await community.identity(db, uid)).username;
      const existing = (await ref.collection('comments').doc(id(data.id)).get()).data();
      if (existing?.authorId === uid && existing.text === message && (existing.parentCommentId||null)===parentCommentId && !existing.deleted) return {};
      if (existing) fail('already-exists', 'This comment was already submitted.');
      const recent = (await db.doc(`feedLimits/${uid}`).get()).data();
      if (recent && clock() - (recent.commentAt || 0) < 3000) fail('resource-exhausted', 'Wait a moment before commenting again.');
      checkedComment = await moderate({text: message, authorName: commentAuthor});
    }
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
        const account = (await tx.get(accountRef)).data() || {};
        if (data.liked !== like.exists) {
          const affinities = {...account.affinities};
          for (const topic of discovery.postTopics(post)) affinities[topic] = Math.max(0, Math.min(100, (affinities[topic] || 0) + (data.liked ? 1 : -1)));
          tx.set(accountRef, {...account, affinities});
          if (data.liked) tx.create(likeRef, {createdAt: clock()}); else tx.delete(likeRef);
          tx.update(ref, {likeCount: Math.max(0, (post.likeCount || 0) + (data.liked ? 1 : -1))});
        }
        return {};
      }
      if (data.action === 'comment') {
        const commentRef = ref.collection('comments').doc(id(data.id));
        const message = text(data.text, 1000, true), authorName = commentAuthor;
        const limitRef = db.doc(`feedLimits/${uid}`);
        const [existing, limits] = await Promise.all([tx.get(commentRef), tx.get(limitRef)]);
        const parent=parentCommentId?(await tx.get(ref.collection('comments').doc(parentCommentId))).data():null;
        if(parentCommentId&&(!parent||parent.deleted))fail('not-found','The comment you are replying to is no longer available.');
        if (existing.exists) {
          if (existing.data().authorId === uid && existing.data().text === message && (existing.data().parentCommentId||null)===parentCommentId && !existing.data().deleted) return {};
          fail('already-exists', 'This comment was already submitted.');
        }
        const now = clock();
        if (limits.exists && now - (limits.data().commentAt || 0) < 3000) fail('resource-exhausted', 'Wait a moment before commenting again.');
        tx.create(commentRef, {text: message, authorName, parentCommentId, replyToName:parent?.authorName||null, moderation: {...checkedComment, checkedAt: now}, authorId: uid, createdAt: now, deleted: false});
        for(const recipient of new Set([post.authorId,parent?.authorId].filter(r=>r&&r!==uid)))
          require('./notifications').notify(tx,db,recipient,`comment:${ref.id}:${commentRef.id}`,{type:recipient===parent?.authorId?'reply':'comment',postId:ref.id,message:recipient===parent?.authorId?'Someone replied to your comment.':'Someone commented on your post.'},now);
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
        const reportRef = ref.collection('reports').doc(uid), limitRef = db.doc(`feedLimits/${uid}`);
        const [existing, limits] = await Promise.all([tx.get(reportRef), tx.get(limitRef)]);
        // A repeated submission or lost response must not reopen a reviewed report.
        if (existing.exists && existing.data().reason === reason) return {};
        const now = clock();
        if (limits.exists && now - (limits.data().reportAt || 0) < 10000) fail('resource-exhausted', 'Wait a few seconds before reporting again.');
        tx.set(reportRef, {reason, reporterId: uid, createdAt: now, resolved: false});
        tx.set(limitRef, {...limits.data(), reportAt: now}); return {};
      }
      fail('invalid-argument', 'Unknown feed action.');
    });
  };
}
function requireVisible(post) { if (!post || post.hidden || post.deleted) fail('not-found', 'This post is no longer available.'); }
function publicPost(doc) {
  const p = doc.data();
  return {id: doc.id, caption: p.caption, authorId: p.authorId, authorName: p.authorName, createdAt: p.createdAt,
    ...(p.businessId?{businessId:p.businessId}:{}),
    recipe: p.recipe, photo: p.photo, likeCount: p.likeCount, commentCount: p.commentCount};
}
module.exports = {createFeedHandler, recipe, photo, cursor};
