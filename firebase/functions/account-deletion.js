const {forgetQuestions}=require('./shopping-questions');
const {anonymizeContributor}=require('./domain');
const {randomUUID,createHash}=require('node:crypto');
const {FieldPath}=require('firebase-admin/firestore');
const {fail}=require('./domain');
const SETTLE_MS=120000;
const roots=['users','feedAccounts','communityProfiles','feedLimits','notifications','householdAccounts','businessAccounts','billingEntitlements','seatEntitlements','familyGrants'];
const hash=s=>createHash('sha256').update(s).digest('hex');
function recent(request,now){
 if(!request.auth)fail('unauthenticated','Sign in before deleting your account.');
 const time=request.auth.token?.auth_time;
 if(!Number.isFinite(time)||time*1000>now+60000||now-time*1000>300000)fail('failed-precondition','Confirm your password again before deleting your account.');
 if(typeof request.auth.uid!=='string'||!request.auth.uid||request.auth.uid.includes('/'))fail('invalid-argument','Invalid account.');
 return request.auth.uid;
}
function createAccountDeletion(db,auth,clock=Date.now){
 async function handle(request){
  const uid=recent(request,clock()),data=request.data;
  if(!data||!['prepare','request'].includes(data.action)||Object.keys(data).some(k=>!['action','confirmation','deleteOwnedSpaces'].includes(k)))fail('invalid-argument','Choose a supported deletion action.');
  const existing=(await db.doc(`accountDeletions/${uid}`).get()).data();
  if(existing&&data.action==='request'&&data.confirmation==='DELETE'&&data.deleteOwnedSpaces===true)return{accepted:true,reference:existing.reference};
  const user=await auth.getUser(uid);if(user.disabled)fail('permission-denied','This account is disabled.');
  const [homes,businesses]=await Promise.all(['households','businesses'].map(c=>db.collection(c).where('owner','==',uid).get()));
  if(data.action==='prepare')return{ownedHouseholds:homes.docs.map(d=>({id:d.id,name:d.data().name||'Household'})),ownedBusinesses:businesses.docs.map(d=>({id:d.id,name:d.data().name||'Creator workspace'}))};
  if(data.confirmation!=='DELETE'||data.deleteOwnedSpaces!==true)fail('invalid-argument','Type DELETE and confirm removal of any shared spaces you own.');
  const ref=db.doc(`accountDeletions/${uid}`);
  return db.runTransaction(async tx=>{
   const previous=(await tx.get(ref)).data();if(previous)return{accepted:true,reference:previous.reference};
   const reference=randomUUID().replaceAll('-','');
   tx.create(ref,{reference,status:'queued',createdAt:clock(),deleteOwnedSpaces:true,cursors:{},leaseUntil:0,retryAfter:clock()});
   return{accepted:true,reference};
  });
 }
 async function process(uid){
  const jobRef=db.doc(`accountDeletions/${uid}`),now=clock();
  let job=await db.runTransaction(async tx=>{
   const value=(await tx.get(jobRef)).data();
   if(!value||value.status==='completed'||value.leaseUntil>now||value.retryAfter>now)return null;
   tx.update(jobRef,{status:'cleaning',leaseUntil:now+540000,retryAfter:now+540000});return value;
  });
  if(!job)return;
  const started=Date.now();
  const budget=()=>{if(Date.now()-started>360000)throw new Error('Deletion continuation required.');};
  const erase=ref=>db.recursiveDelete(ref);
  async function eraseQuery(query){for(;;){budget();const page=await query.limit(100).get();if(!page.size)return;for(const doc of page.docs)await erase(doc.ref);}}
  async function scan(label,query,action){
   let cursor=job.cursors?.[label];if(cursor==='__done__')return;
   for(;;){budget();let q=query.orderBy(FieldPath.documentId()).limit(100);if(cursor)q=q.startAfter(cursor);
    const page=await q.get();if(!page.size){cursor='__done__';}else{for(const doc of page.docs)await action(doc);cursor=page.docs.at(-1).id;}
    job.cursors={...job.cursors,[label]:cursor};await jobRef.update({cursors:job.cursors});if(cursor==='__done__')return;
   }
  }
  async function cleanHouse(ref){
   const home=(await ref.get()).data();if(!home)return;
   if(home.owner===uid){
    if(!job.deleteOwnedSpaces)throw new Error('Owner confirmation missing.');
    for(const member of Object.keys(home.members||{}))await db.runTransaction(async tx=>{const r=db.doc(`householdAccounts/${member}`),v=(await tx.get(r)).data();if(v?.householdId===ref.id)tx.delete(r);});
    await eraseQuery(db.collection('householdInvites').where('householdId','==',ref.id));await erase(ref);
   }else await db.runTransaction(async tx=>{const v=(await tx.get(ref)).data();if(!v)return;const listRef=ref.collection('lists').doc('shopping');const list=(await tx.get(listRef)).data();const questionsRef=ref.collection('conversations').doc('shopping'),questions=(await tx.get(questionsRef)).data();if(questions)tx.set(questionsRef,forgetQuestions(questions,uid));if(list)tx.set(listRef,{...list,items:anonymizeContributor(list.items,uid)});const members={...v.members};delete members[uid];const roles={...v.roles};delete roles[uid];const memberColors={...v.memberColors};delete memberColors[uid];const requestRates={...v.requestRates};delete requestRates[uid];const requests=Object.fromEntries(Object.entries(v.requests||{}).filter(([,r])=>r.authorId!==uid).map(([id,r])=>[id,r.resolvedBy===uid?{...r,resolvedBy:'deleted-user'}:r]));tx.update(ref,{members,roles,memberColors,requestRates,requests});});
  }
  async function cleanBusiness(ref){
   const business=(await ref.get()).data();if(!business)return;
   if(business.owner!==uid){await db.runTransaction(async tx=>{const b=(await tx.get(ref)).data();if(!b)return;const members={...b.members};delete members[uid];tx.update(ref,{members});});return;}
   if(!job.deleteOwnedSpaces)throw new Error('Owner confirmation missing.');
   await scan('businessAccounts-'+ref.id,db.collection('businessAccounts'),async doc=>{if((doc.data().ids||[]).includes(ref.id))await db.runTransaction(async tx=>{const v=(await tx.get(doc.ref)).data();if(v)tx.update(doc.ref,{ids:(v.ids||[]).filter(id=>id!==ref.id)});});});
   for(const collection of ['feedPosts','businessCollections','businessCampaigns','businessFollows','businessInvites','businessEvents','promotionPurchases'])await eraseQuery(db.collection(collection).where('businessId','==',ref.id));
   for(const collection of ['businessWallets','businessMetrics'])await erase(db.doc(`${collection}/${ref.id}`));await erase(ref);
  }
  try{
   try{await auth.updateUser(uid,{disabled:true});await auth.revokeRefreshTokens(uid);}catch(e){if(e.code!=='auth/user-not-found')throw e;}
   // Recursive deletes include inline recipe photos, lists and week subcollections.
   const member=(await db.doc(`householdAccounts/${uid}`).get()).data();
   if(member?.householdId)await cleanHouse(db.doc(`households/${member.householdId}`));
   await scan('owned-homes',db.collection('households').where('owner','==',uid),doc=>cleanHouse(doc.ref));
   const spaces=(await db.doc(`businessAccounts/${uid}`).get()).data();
   for(const id of spaces?.ids||[])await cleanBusiness(db.doc(`businesses/${id}`));
   await scan('owned-businesses',db.collection('businesses').where('owner','==',uid),doc=>cleanBusiness(doc.ref));
   await eraseQuery(db.collection('feedPosts').where('authorId','==',uid));
   for(;;){
    budget();const comments=await db.collectionGroup('comments').where('authorId','==',uid).limit(100).get();if(!comments.size)break;
    for(const comment of comments.docs){
     const parentPath=comment.ref.path.split('/').slice(0,-1).join('/');
     await scan('reply-names-'+hash(comment.ref.path),db.collection(parentPath).where('parentCommentId','==',comment.id),reply=>reply.ref.update({replyToName:'Deleted account'}));
     await erase(comment.ref);
    }
   }
   await eraseQuery(db.collectionGroup('reports').where('reporterId','==',uid));
   await scan('post-likes',db.collection('feedPosts'),async doc=>{
    const like=doc.ref.collection('likes').doc(uid);if((await like.get()).exists)await db.runTransaction(async tx=>{const [p,l]=await Promise.all([tx.get(doc.ref),tx.get(like)]);if(l.exists){tx.delete(like);if(p.exists)tx.update(doc.ref,{likeCount:Math.max(0,(p.data().likeCount||0)-1)});}});
   });
   await scan('following',db.collection('feedAccounts'),async doc=>{
    if(Object.hasOwn(doc.data().following||{},uid))await db.runTransaction(async tx=>{const v=(await tx.get(doc.ref)).data();if(!v)return;const following={...v.following};delete following[uid];tx.update(doc.ref,{following});});
   });
   // Legacy follow records contain only a hash of uid|businessId; no uid field.
   await scan('business-follows',db.collection('businessFollows'),async doc=>{if(typeof doc.data().businessId==='string'&&doc.id===hash(uid+'|'+doc.data().businessId))await erase(doc.ref);});
   for(const [collection,field] of [['notificationDevices','uid'],['billingPurchases','uid'],['promotionPurchases','uid'],['promotionRequests','owner'],['promotionReceipts','owner'],['promotionReceipts','uid'],['promotionCampaigns','owner'],['impressionTickets','uid'],['businessInvites','targetUid'],['businessInvites','invitedBy']])await eraseQuery(db.collection(collection).where(field,'==',uid));
   // Security/admin history remains useful without retaining this account identity.
   for(const field of ['actor','target'])await scan('audit-'+field,db.collection('adminAudit').where(field,'==',uid),async doc=>{const value={...doc.data(),[field]:'deleted-account'};delete value.reason;delete value.before;delete value.after;await doc.ref.set(value);});
   for(const collection of roots)await erase(db.doc(`${collection}/${uid}`));
   if(clock()<job.createdAt+SETTLE_MS){await jobRef.update({status:'settling',retryAfter:job.createdAt+SETTLE_MS,leaseUntil:0,cursors:{}});return;}
   try{await auth.deleteUser(uid);}catch(e){if(e.code!=='auth/user-not-found')throw e;}
   await jobRef.set({reference:job.reference,status:'completed',createdAt:job.createdAt,completedAt:clock(),leaseUntil:0,deleteAfter:new Date(clock()+30*86400000)});
  }catch(error){await jobRef.update({status:'retry',leaseUntil:0,retryAfter:clock()+60000,errorCode:'CLEANUP_RETRY'});throw error;}
 }
 return{handle,process};
}
module.exports={createAccountDeletion,recent,SETTLE_MS};
