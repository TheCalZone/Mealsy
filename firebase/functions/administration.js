const {randomUUID} = require('node:crypto');
const {FieldPath} = require('firebase-admin/firestore');
const {fail} = require('./domain');
const {requestData} = require('./request');
const {cursor} = require('./feed');
function identifier(value) {
  if (typeof value !== 'string' || !/^[a-zA-Z0-9_-]{1,128}$/.test(value)) fail('invalid-argument', 'Invalid identifier.');
  return value;
}
function createAdministration(db, auth, clock = Date.now, feed = require('./feed').createFeedHandler(db)) {
  return async request => {
    if (!request.auth) fail('unauthenticated', 'Sign in to access administration.');
    const data = requestData(request, ['status','lookupUser','posts','setPostHidden','resolveReport','pausePromotion','setFamilyGrant','verifyEmail','audit']);
    // Check the current Auth record on every call: an old ID token cannot retain
    // admin access after the administrator role has been removed.
    const actor = await auth.getUser(request.auth.uid);
    const allowed = !actor.disabled && actor.customClaims?.admin === true;
    if (data.action === 'status') return {admin: allowed};
    if (!allowed) fail('permission-denied', 'Administrator access is required.');
    const uid = request.auth.uid;
    if (data.action === 'lookupUser') {
      if (typeof data.email !== 'string' || !data.email.trim() || data.email.length > 320) fail('invalid-argument', 'Enter an account email address.');
      let user;
      try { user = await auth.getUserByEmail(data.email.trim()); }
      catch (error) { if (error.code === 'auth/user-not-found') fail('not-found', 'No account uses that email address.'); throw error; }
      const [profile, grant, entitlement] = await Promise.all(['communityProfiles','familyGrants','billingEntitlements'].map(name => db.doc(`${name}/${user.uid}`).get()));
      return {uid: user.uid, email: user.email || '', username: profile.data()?.username || '', emailVerified: user.emailVerified === true,
        disabled: user.disabled === true, admin: user.customClaims?.admin === true,
        grantExpiresAt: grant.data()?.active === true ? grant.data().expiresAt || 0 : 0,
        canVerifyEmail:true,subscriptionExpiresAt: entitlement.data()?.active === true ? entitlement.data().expiresAt || 0 : 0};
    }
    if (data.action === 'posts') {
      // Existing moderation query returns public snapshots and bounded report pages.
      const result = await feed({auth: {uid, token: {feedModerator: true}}, data: {action: 'moderation', cursor: data.cursor}});
      result.posts = await Promise.all(result.posts.map(async post => ({...post,
        promotionActive: (await db.doc(`promotionCampaigns/${post.id}`).get()).data()?.active === true})));
      return result;
    }
    if (data.action === 'audit') {
      const next = cursor(data.cursor);
      let query = db.collection('adminAudit').orderBy('createdAt','desc').orderBy(FieldPath.documentId(),'desc');
      if (next) query = query.startAfter(next.createdAt,next.id);
      const snapshot = await query.limit(20).get(), last = snapshot.docs.at(-1);
      return {entries: snapshot.docs.map(d => ({id:d.id,...d.data()})), cursor: snapshot.size === 20 ? {createdAt:last.data().createdAt,id:last.id}:null};
    }
    if (typeof data.reason !== 'string' || !data.reason.trim() || data.reason.length > 500) fail('invalid-argument', 'Add a short reason for this change (up to 500 characters).');
    const auditRef = db.doc(`adminAudit/${randomUUID()}`);
    let target, after;
    if(data.action==='verifyEmail'){
      target=identifier(data.uid);
      const user=await auth.getUser(target);
      if(user.disabled||!user.email)fail('failed-precondition','Choose an enabled account with an email address.');
      if(typeof data.email!=='string'||data.email.trim().toLowerCase()!==user.email.toLowerCase())fail('failed-precondition','The account email changed. Look up the account again before confirming verification.');
      if(user.emailVerified)return{emailVerified:true};
      await db.runTransaction(async tx=>{
        if((await tx.get(db.doc(`accountDeletions/${target}`))).exists)fail('failed-precondition','This account is being deleted.');
        tx.create(auditRef,{actor:uid,action:data.action,target,reason:data.reason.trim(),createdAt:clock(),before:{emailVerified:false},after:{emailVerified:true},outcome:'started'});
      });
      try{
        const updated=await auth.updateUser(target,{emailVerified:true});
        if(updated.email?.toLowerCase()!==user.email.toLowerCase()){await auth.updateUser(target,{emailVerified:false});fail('failed-precondition','The email changed during verification. It was left unconfirmed; review the account before sharing access.');}
        await db.runTransaction(async tx=>tx.update(auditRef,{outcome:'completed',completedAt:clock()}));
        return{emailVerified:true};
      }catch(error){
        await db.runTransaction(async tx=>tx.update(auditRef,{outcome:'unconfirmed',completedAt:clock()}));
        throw error;
      }
    }
    if (data.action === 'setFamilyGrant') {
      target = identifier(data.uid);
      if (!Number.isInteger(data.days) || data.days < 0 || data.days > 365) fail('invalid-argument', 'Choose 0 to revoke or 1–365 days of Family access.');
      await auth.getUser(target); // No grants for nonexistent accounts.
    } else target = identifier(data.postId);
    return db.runTransaction(async tx => {
      const now = clock();
      if (data.action === 'setFamilyGrant') {
        const ref = db.doc(`familyGrants/${target}`), previous = (await tx.get(ref)).data();
        if(data.days>0&&(await tx.get(db.doc(`accountDeletions/${target}`))).exists)fail('failed-precondition','This account is being deleted.');
        after = {active:data.days > 0,expiresAt:data.days > 0 ? now + data.days * 86400000 : 0,grantedAt:now,source:'administrator'};
        tx.set(ref, after);
        tx.create(auditRef,{actor:uid,action:data.action,target,reason:data.reason.trim(),createdAt:now,before:{active:previous?.active === true,expiresAt:previous?.expiresAt || 0},after});
        return {};
      }
      const ref = db.doc(`feedPosts/${target}`), post = (await tx.get(ref)).data();
      if (!post) fail('not-found', 'This post no longer exists.');
      let before;
      if (data.action === 'setPostHidden') {
        if (typeof data.hidden !== 'boolean' || post.deleted) fail('invalid-argument', 'Choose an existing post to hide or restore.');
        // Hiding is always allowed; old/unapproved content cannot be restored by
        // an administrator to bypass automatic prepublication moderation.
        if (!data.hidden && post.moderation?.status !== 'approved') fail('failed-precondition', 'This post needs automatic safety approval before it can be restored.');
        before = {hidden:post.hidden === true}; after = {hidden:data.hidden}; tx.update(ref,after);
      } else if (data.action === 'resolveReport') {
        const reportRef = ref.collection('reports').doc(identifier(data.reporterId)), report = (await tx.get(reportRef)).data();
        if (!report) fail('not-found','This report no longer exists.');
        before = {resolved:report.resolved === true}; after = {resolved:true}; tx.update(reportRef,after);
      } else if (data.action === 'pausePromotion') {
        const campaignRef = db.doc(`promotionCampaigns/${target}`), campaign = (await tx.get(campaignRef)).data();
        if (!campaign) fail('not-found','This post has no promotion campaign.');
        before = {active:campaign.active === true}; after = {active:false}; tx.update(campaignRef,after);
      }
      tx.create(auditRef,{actor:uid,action:data.action,target,reason:data.reason.trim(),createdAt:now,before,after,
        ...(data.action === 'resolveReport' ? {reporterId:data.reporterId}: {})});
      return {};
    });
  };
}
module.exports = {createAdministration};
