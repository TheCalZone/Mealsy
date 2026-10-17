const {randomUUID,randomBytes,createHash}=require('node:crypto');
const {FieldPath}=require('firebase-admin/firestore');
const {fail}=require('./domain');const {requestData}=require('./request');
const {recipe,photo,cursor,createFeedHandler}=require('./feed');const {requireRole}=require('./business-access');
const hash=s=>createHash('sha256').update(s).digest('hex');
function id(value){if(typeof value!=='string'||!/^[a-zA-Z0-9_-]{1,100}$/.test(value))fail('invalid-argument','Invalid identifier.');return value;}
function text(value,max,required=false){if(typeof value!=='string'||value.length>max||(required&&!value.trim()))fail('invalid-argument','Keep text within the displayed limits.');return value.trim();}
function url(value){if(!value)return'';try{const u=new URL(text(value,1000));if(u.protocol==='https:'&&!u.username&&!u.password)return u.href;}catch{}fail('invalid-argument','Use a complete https website link without login details.');}
function profile(b,id){return{id,name:b.name,description:b.description,website:b.website,logo:b.logo||'',kind:b.kind,verified:b.verified===true};}
function publicPost(doc){const p=doc.data();return{id:doc.id,caption:p.caption,authorId:p.authorId,authorName:p.authorName,businessId:p.businessId,createdAt:p.createdAt,recipe:p.recipe,photo:p.photo,likeCount:p.likeCount||0,commentCount:p.commentCount||0};}
function visible(p,businessId){if(!p||p.businessId!==businessId||p.deleted||p.hidden||!p.recipe||p.moderation?.status!=='approved')fail('failed-precondition','Choose visible, safety-approved recipes from this business.');}
function collectionInput(data){
  if(!Array.isArray(data.postIds)||data.postIds.length>30)fail('invalid-argument','Collections support up to 30 recipes.');
  const postIds=[...new Set(data.postIds.map(id))];
  if(!Array.isArray(data.meals)||data.meals.length>21)fail('invalid-argument','Weekly plans support up to 21 meal slots.');
  const slots=new Set();const meals=data.meals.map(m=>{
    if(!m||!Number.isInteger(m.day)||m.day<0||m.day>6||!Number.isInteger(m.slot)||m.slot<0||m.slot>2||!postIds.includes(m.postId)||slots.has(`${m.day}:${m.slot}`))fail('invalid-argument','Choose one collection recipe per day and meal slot.');
    slots.add(`${m.day}:${m.slot}`);return{day:m.day,slot:m.slot,postId:m.postId};
  });
  return{title:text(data.title,120,true),description:text(data.description||'',1000),postIds,meals,
    offer:{label:text(data.offer?.label||'',100),url:url(data.offer?.url||''),code:text(data.offer?.code||'',80)},published:data.published===true};
}
function createBusiness(db,auth,clock=Date.now,moderate=require('./moderation').moderate){
  const feed=createFeedHandler(db,clock,moderate);
  return async request=>{
    if(!request.auth)fail('unauthenticated','Sign in to use business and creator accounts.');
    const data=requestData(request,['mine','create','updateProfile','workspace','invite','join','removeMember','revokeInvite','publishRecipe','hideRecipe','saveCollection','collection','directory','publicProfile','follow','track','analytics','saveCampaign','pauseCampaign','adminBusinesses','verifyBusiness']);
    const uid=request.auth.uid,user=await auth.getUser(uid);if(user.disabled)fail('permission-denied','This account is disabled.');
    const accountRef=db.doc(`businessAccounts/${uid}`);
    if(data.action==='mine'){
      const ids=(await accountRef.get()).data()?.ids||[];
      const docs=await Promise.all(ids.map(b=>db.doc(`businesses/${id(b)}`).get()));
      return{businesses:docs.filter(d=>d.data()?.members?.[uid]).map(d=>({...profile(d.data(),d.id),role:d.data().members[uid]}))};
    }
    if(data.action==='directory'||data.action==='adminBusinesses'){
      if(data.action==='adminBusinesses'&&user.customClaims?.admin!==true)fail('permission-denied','Administrator access is required.');
      const next=cursor(data.cursor);let query=db.collection('businesses').orderBy('createdAt','desc').orderBy(FieldPath.documentId(),'desc');
      if(next)query=query.startAfter(next.createdAt,next.id);
      const docs=await query.limit(20).get(),last=docs.docs.at(-1);
      return{businesses:docs.docs.filter(d=>data.action==='adminBusinesses'||d.data().verified===true).map(d=>({...profile(d.data(),d.id),...(data.action==='adminBusinesses'?{owner:d.data().owner}: {})})),cursor:docs.size===20?{createdAt:last.data().createdAt,id:last.id}:null};
    }
    if(data.action==='create'){
      if(!user.emailVerified)fail('failed-precondition','Confirm your email before creating a creator or business account.');
      const body={name:text(data.name,80,true),description:text(data.description||'',1000),website:url(data.website||''),logo:photo(data.logo||''),kind:data.kind};
      if(!['creator','organisation'].includes(body.kind))fail('invalid-argument','Choose independent creator or organisation.');
      await moderate({...body,logo:undefined,photo:body.logo});const businessId=randomUUID();
      await db.runTransaction(async tx=>{
        const account=(await tx.get(accountRef)).data()||{},ids=account.ids||[];
        if(ids.length>=10)fail('resource-exhausted','You can belong to up to 10 creator or business accounts.');
        tx.create(db.doc(`businesses/${businessId}`),{...body,owner:uid,members:{[uid]:'owner'},verified:false,createdAt:clock(),updatedAt:clock()});tx.set(accountRef,{ids:[...ids,businessId]});
      });return{id:businessId};
    }
    if(data.action==='join'){
      if(!user.emailVerified)fail('failed-precondition','Confirm your email before accepting team invitations.');
      const code=text(data.code,100,true),inviteRef=db.doc(`businessInvites/${hash(code)}`);
      return db.runTransaction(async tx=>{
        const invite=(await tx.get(inviteRef)).data();
        if(!invite||invite.used||invite.revoked||invite.expiresAt<=clock()||invite.targetUid!==uid)fail('permission-denied','This invitation is invalid, expired or belongs to another account.');
        const ref=db.doc(`businesses/${id(invite.businessId)}`),b=(await tx.get(ref)).data(),account=(await tx.get(accountRef)).data()||{};
        if(!b||b.members?.[invite.invitedBy]!=='owner')fail('permission-denied','This invitation is no longer valid.');
        if(Object.keys(b.members).length>=20&&!b.members[uid]||(account.ids||[]).length>=10&&!(account.ids||[]).includes(ref.id))fail('resource-exhausted','The team or account membership limit has been reached.');
        if(b.owner===uid)fail('failed-precondition','You already own this account.');
        tx.update(ref,{members:{...b.members,[uid]:invite.role}});tx.set(accountRef,{ids:[...new Set([...(account.ids||[]),ref.id])]});tx.update(inviteRef,{used:true});return{id:ref.id};
      });
    }
    if(data.action==='collection'){
      const ref=db.doc(`businessCollections/${id(data.collectionId)}`),c=(await ref.get()).data();
      if(!c||!c.published)fail('not-found','This collection is unavailable.');
      const b=(await db.doc(`businesses/${c.businessId}`).get()).data();if(b?.verified!==true)fail('not-found','This collection is unavailable.');
      const posts=await Promise.all(c.postIds.map(postId=>db.doc(`feedPosts/${postId}`).get()));
      const valid=posts.filter(p=>p.exists&&!p.data().hidden&&!p.data().deleted&&p.data().businessId===c.businessId&&p.data().moderation?.status==='approved');
      return{collection:{id:ref.id,...c,postIds:valid.map(p=>p.id),meals:c.meals.filter(m=>valid.some(p=>p.id===m.postId))},business:profile(b,c.businessId),posts:valid.map(publicPost)};
    }
    const businessId=id(data.businessId),ref=db.doc(`businesses/${businessId}`),b=(await ref.get()).data();
    if(data.action==='publicProfile'){
      if(b?.verified!==true)fail('not-found','This business profile is unavailable.');
      const collections=await db.collection('businessCollections').where('businessId','==',businessId).limit(50).get();
      const follow=(await db.doc(`businessFollows/${hash(uid+'|'+businessId)}`).get()).exists;
      return{business:profile(b,businessId),following:follow,collections:collections.docs.filter(d=>d.data().published).map(d=>({id:d.id,...d.data()}))};
    }
    if(data.action==='follow'){
      if(b?.verified!==true||typeof data.following!=='boolean')fail('invalid-argument','Choose a verified account and follow state.');
      const followRef=db.doc(`businessFollows/${hash(uid+'|'+businessId)}`);
      return db.runTransaction(async tx=>{
        const before=(await tx.get(followRef)).exists,metricsRef=db.doc(`businessMetrics/${businessId}`),metrics=(await tx.get(metricsRef)).data()||{};
        if(before!==data.following){if(data.following)tx.set(followRef,{businessId,createdAt:clock()});else tx.delete(followRef);tx.set(metricsRef,{...metrics,followers:Math.max(0,(metrics.followers||0)+(data.following?1:-1))});}return{};
      });
    }
    if(data.action==='track'){
      if(b?.verified!==true)return{};
      if(!['profileViews','collectionOpens','recipeOpens','saves','clicks','planSaves','postViews'].includes(data.event))fail('invalid-argument','Unknown aggregate event.');
      if(b.members?.[uid])return{};
      let target=businessId;
      if(['collectionOpens','planSaves','clicks'].includes(data.event)){
        target=id(data.targetId);const c=(await db.doc(`businessCollections/${target}`).get()).data();if(!c?.published||c.businessId!==businessId)fail('not-found','Collection unavailable.');
      }else if(['recipeOpens','saves','postViews'].includes(data.event)){
        target=id(data.targetId);visible((await db.doc(`feedPosts/${target}`).get()).data(),businessId);
      }
      const day=new Date(clock()).toISOString().slice(0,10),dedup=db.doc(`businessEvents/${hash([uid,businessId,data.event,target,day].join('|'))}`),metricsRef=db.doc(`businessMetrics/${businessId}/days/${day}`);
      return db.runTransaction(async tx=>{
        const previous=await tx.get(dedup),metrics=(await tx.get(metricsRef)).data()||{};if(previous.exists)return{};
        tx.create(dedup,{createdAt:clock(),deleteAfter:new Date(clock()+90*86400000)});tx.set(metricsRef,{...metrics,[data.event]:(metrics[data.event]||0)+1});return{};
      });
    }
    if(data.action==='verifyBusiness'){
      if(user.customClaims?.admin!==true)fail('permission-denied','Administrator access is required.');
      if(typeof data.verified!=='boolean')fail('invalid-argument','Choose a verification state.');const reason=text(data.reason,500,true);
      return db.runTransaction(async tx=>{
        const current=(await tx.get(ref)).data();if(!current)fail('not-found','Business unavailable.');
        tx.update(ref,{verified:data.verified,verifiedAt:clock(),verifiedBy:uid});tx.create(db.doc(`adminAudit/${randomUUID()}`),{actor:uid,action:'verifyBusiness',target:businessId,reason,createdAt:clock(),before:{verified:current.verified===true},after:{verified:data.verified}});return{};
      });
    }
    const role=b?.members&&Object.hasOwn(b.members,uid)?b.members[uid]:null;if(!role)fail('permission-denied','You are no longer on this team.');
    if(data.action==='workspace'){
      const collections=await db.collection('businessCollections').where('businessId','==',businessId).limit(50).get();
      const campaigns=await db.collection('businessCampaigns').where('businessId','==',businessId).limit(50).get();
      const posts=await db.collection('feedPosts').where('businessId','==',businessId).limit(100).get();
      const invites=role==='owner'?await db.collection('businessInvites').where('businessId','==',businessId).limit(50).get():null;
      const memberLabels=role==='owner'?await Promise.all(Object.entries(b.members).map(async([uid,role])=>{try{const u=await auth.getUser(uid);return{uid,role,label:u.displayName||u.email||'Team member'};}catch{return{uid,role,label:'Account unavailable'};}})):[];
      return{business:profile(b,businessId),role,members:memberLabels,
        invitations:invites?.docs.filter(d=>!d.data().used&&!d.data().revoked&&d.data().expiresAt>clock()).map(d=>({id:d.id,targetUid:d.data().targetUid,role:d.data().role}))||[],
        collections:collections.docs.map(d=>({id:d.id,...d.data()})),campaigns:campaigns.docs.map(d=>({id:d.id,...d.data()})),posts:posts.docs.filter(d=>!d.data().deleted).map(d=>({...publicPost(d),hidden:!!d.data().hidden})),checkoutEnabled:false};
    }
    if(data.action==='analytics'){
      if(!['owner','campaignManager'].includes(role))fail('permission-denied','Campaign manager or owner access is required.');
      const dayIds=Array.from({length:14},(_,i)=>new Date(clock()-i*86400000).toISOString().slice(0,10));
      const days=await Promise.all(dayIds.map(day=>db.doc(`businessMetrics/${businessId}/days/${day}`).get()));
      return{followers:(await db.doc(`businessMetrics/${businessId}`).get()).data()?.followers||0,days:days.map((d,i)=>({day:dayIds[i],...d.data()})),estimated:true};
    }
    if(data.action==='invite'){
      requireRole(b,uid,'owner');if(!['editor','campaignManager'].includes(data.role))fail('invalid-argument','Choose editor or campaign manager.');
      let target;try{target=await auth.getUserByEmail(text(data.email,320,true));}catch(e){if(e.code==='auth/user-not-found')fail('not-found','Ask this person to create a Mealsy account first.');throw e;}
      const code=randomBytes(18).toString('hex'),inviteRef=db.doc(`businessInvites/${hash(code)}`);
      await db.runTransaction(async tx=>{requireRole((await tx.get(ref)).data(),uid,'owner');const pending=await tx.get(db.collection('businessInvites').where('businessId','==',businessId).limit(50));if(pending.size>=50)fail('resource-exhausted','There are too many recent team invitations. Revoke unused invitations or retry after they expire.');tx.create(inviteRef,{businessId,targetUid:target.uid,role:data.role,invitedBy:uid,expiresAt:clock()+48*3600000,deleteAfter:new Date(clock()+48*3600000),used:false,revoked:false});});return{code};
    }
    if(data.action==='revokeInvite'){
      const inviteRef=db.doc(`businessInvites/${id(data.inviteId)}`);
      return db.runTransaction(async tx=>{requireRole((await tx.get(ref)).data(),uid,'owner');const invite=(await tx.get(inviteRef)).data();if(invite?.businessId!==businessId)fail('not-found','Invitation unavailable.');tx.update(inviteRef,{revoked:true});return{};});
    }
    if(data.action==='removeMember'){
      const member=id(data.uid),memberRef=db.doc(`businessAccounts/${member}`);
      return db.runTransaction(async tx=>{
        const current=requireRole((await tx.get(ref)).data(),uid,'owner'),account=(await tx.get(memberRef)).data()||{};
        if(member===current.owner)fail('failed-precondition','The owner cannot be removed.');const members={...current.members};delete members[member];
        tx.update(ref,{members});tx.set(memberRef,{ids:(account.ids||[]).filter(b=>b!==businessId)});return{};
      });
    }
    if(data.action==='updateProfile'){
      requireRole(b,uid,'owner');const body={name:text(data.name,80,true),description:text(data.description||'',1000),website:url(data.website||''),logo:photo(data.logo||'')};
      await moderate({...body,logo:undefined,photo:body.logo});
      return db.runTransaction(async tx=>{const current=requireRole((await tx.get(ref)).data(),uid,'owner');tx.update(ref,{...body,verified:current.verified===true&&current.name===body.name&&current.website===body.website,updatedAt:clock()});return{};});
    }
    if(data.action==='publishRecipe'){
      requireRole(b,uid,'editor',true);if(!user.emailVerified)fail('failed-precondition','Confirm your email before publishing.');
      return feed({businessPublishingAuthorized:true,auth:{uid,token:{email_verified:true}},data:{action:'publish',id:id(data.id),businessId,caption:data.caption||'',recipe:recipe(data.recipe),photo:photo(data.photo||'')}});
    }
    if(data.action==='hideRecipe'){
      const postRef=db.doc(`feedPosts/${id(data.postId)}`);
      return db.runTransaction(async tx=>{requireRole((await tx.get(ref)).data(),uid,'editor');const post=(await tx.get(postRef)).data();if(post?.businessId!==businessId||post.deleted)fail('not-found','Recipe unavailable.');if(typeof data.hidden!=='boolean')fail('invalid-argument','Choose a visibility state.');if(!data.hidden&&post.moderation?.status!=='approved')fail('permission-denied','Safety approval is required.');tx.update(postRef,{hidden:data.hidden});return{};});
    }
    if(data.action==='saveCollection'){
      requireRole(b,uid,'editor');const collectionId=id(data.id),body=collectionInput(data);if(body.published&&!body.postIds.length)fail('invalid-argument','Add at least one recipe before publishing.');
      await moderate({title:body.title,description:body.description,offer:body.offer});
      return db.runTransaction(async tx=>{
        requireRole((await tx.get(ref)).data(),uid,'editor',body.published);const cRef=db.doc(`businessCollections/${collectionId}`),previous=(await tx.get(cRef)).data();
        if(previous&&previous.businessId!==businessId)fail('permission-denied','This collection belongs to another account.');
        if(!previous&&(await tx.get(db.collection('businessCollections').where('businessId','==',businessId).limit(50))).size>=50)fail('resource-exhausted','This account supports 50 collections. Edit an existing collection.');
        const list=await Promise.all(body.postIds.map(postId=>tx.get(db.doc(`feedPosts/${postId}`))));for(const p of list)visible(p.data(),businessId);
        tx.set(cRef,{...body,businessId,createdAt:previous?.createdAt||clock(),updatedAt:clock()});return{id:collectionId};
      });
    }
    if(data.action==='saveCampaign'){
      requireRole(b,uid,'campaign');const campaignId=id(data.id),postId=id(data.postId),name=text(data.name,100,true);
      if(!Number.isSafeInteger(data.budgetMinor)||data.budgetMinor<100||data.budgetMinor>1000000||!Number.isSafeInteger(data.dailyBudgetMinor)||data.dailyBudgetMinor<100||data.dailyBudgetMinor>data.budgetMinor||!Number.isSafeInteger(data.startsAt)||!Number.isSafeInteger(data.endsAt)||data.endsAt!==0&&(data.endsAt<=data.startsAt||data.endsAt<=clock()||data.endsAt-data.startsAt>90*86400000))fail('invalid-argument','Use a £1–£10,000 spending limit and a daily limit within it. Choose no end date or an end date within 90 days.');
      return db.runTransaction(async tx=>{
        requireRole((await tx.get(ref)).data(),uid,'campaign',true);const campaignRef=db.doc(`businessCampaigns/${campaignId}`),previous=(await tx.get(campaignRef)).data(),post=(await tx.get(db.doc(`feedPosts/${postId}`))).data();
        visible(post,businessId);if(previous&&(previous.businessId!==businessId||previous.status==='active'))fail('failed-precondition','Pause active campaigns before changing them.');
        if(!previous&&(await tx.get(db.collection('businessCampaigns').where('businessId','==',businessId).limit(50))).size>=50)fail('resource-exhausted','This account supports 50 campaigns. Edit an existing campaign.');
        tx.set(campaignRef,{businessId,name,postId,budgetMinor:data.budgetMinor,dailyBudgetMinor:data.dailyBudgetMinor,currency:'GBP',cpmMicro:1000000,startsAt:data.startsAt,endsAt:data.endsAt,status:'awaiting-payment-integration',spentMinor:previous?.spentMinor||0,spentMicro:previous?.spentMicro||0,impressions:previous?.impressions||0,createdAt:previous?.createdAt||clock(),updatedAt:clock()});return{checkoutEnabled:false};
      });
    }
    if(data.action==='pauseCampaign'){
      const campaignRef=db.doc(`businessCampaigns/${id(data.campaignId)}`);
      return db.runTransaction(async tx=>{
        requireRole((await tx.get(ref)).data(),uid,'campaign');const campaign=(await tx.get(campaignRef)).data();if(campaign?.businessId!==businessId)fail('not-found','Campaign unavailable.');
        const promotionRef=db.doc(`promotionCampaigns/${campaign.postId}`),promotion=(await tx.get(promotionRef)).data();
        tx.update(campaignRef,{status:'paused',updatedAt:clock()});if(promotion?.businessCampaignId===campaignRef.id)tx.update(promotionRef,{active:false});return{};
      });
    }
    fail('invalid-argument','Unknown business action.');
  };
}
module.exports={createBusiness,collectionInput,url};
