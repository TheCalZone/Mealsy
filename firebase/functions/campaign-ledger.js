const {createHash}=require('node:crypto');const {fail}=require('./domain');const {requireRole}=require('./business-access');const {requestData}=require('./request');
// All money uses integer micro-pounds: £1 = 1,000,000. No floating point debits.
const CPM=1000000,IMPRESSION_COST=CPM/1000;
const hash=s=>createHash('sha256').update(s).digest('hex');
function eligible(c,now){return c?.status==='active'&&c.startsAt<=now&&(!c.endsAt||c.endsAt>now)&&(c.spentMicro||0)+IMPRESSION_COST<=c.budgetMinor*10000;}
function createCampaignLedger(db,clock=Date.now){return async request=>{
 if(!request.auth)fail('unauthenticated','Sign in to manage promotions.');const uid=request.auth.uid,data=requestData(request,['wallet','verifyCredit','start','prepareImpression','impression']);
 const config=(await db.doc('commercialConfig/campaigns').get()).data();
 if(data.action==='wallet'){
  const b=(await db.doc(`businesses/${safe(data.businessId)}`).get()).data();requireRole(b,uid,'campaign');const wallet=(await db.doc(`businessWallets/${data.businessId}`).get()).data();
  return{balanceMicro:wallet?.balanceMicro||0,cpmMicro:CPM,checkoutEnabled:config?.enabled===true};
 }
 if(config?.enabled!==true)fail('failed-precondition','Promotion purchases are not enabled yet. Your campaign can be saved as a draft.');
 if(data.action==='verifyCredit')return require('./promotion-purchases').createPromotionPurchases(db).verify(uid,data.businessId,data.token);
 if(['prepareImpression','impression'].includes(data.action)&&!request.app)fail('failed-precondition','This device must pass app verification before impressions can be billed.');
 if(data.action==='start'){
  const campaignRef=db.doc(`businessCampaigns/${safe(data.campaignId)}`);
  return db.runTransaction(async tx=>{
   const c=(await tx.get(campaignRef)).data();if(!c)fail('not-found','Campaign unavailable.');const b=(await tx.get(db.doc(`businesses/${c.businessId}`))).data();requireRole(b,uid,'campaign',true);
   const wallet=(await tx.get(db.doc(`businessWallets/${c.businessId}`))).data(),post=(await tx.get(db.doc(`feedPosts/${c.postId}`))).data(),placement=(await tx.get(db.doc(`promotionCampaigns/${c.postId}`))).data();
   if(placement?.active&&placement.businessCampaignId!==campaignRef.id)fail('failed-precondition','Pause the other campaign for this recipe first.');
   if(!post?.recipe||post.businessId!==c.businessId||post.hidden||post.deleted||post.moderation?.status!=='approved')fail('failed-precondition','Choose an available approved recipe.');
   if((wallet?.balanceMicro||0)<IMPRESSION_COST||c.endsAt&&c.endsAt<=clock()||(c.spentMicro||0)+IMPRESSION_COST>c.budgetMinor*10000)fail('failed-precondition','Add credits or increase your campaign budget before starting.');
   tx.update(campaignRef,{status:'active',cpmMicro:CPM,updatedAt:clock()});tx.set(db.doc(`promotionCampaigns/${c.postId}`),{active:true,businessCampaignId:campaignRef.id,businessId:c.businessId,postId:c.postId,owner:post.authorId,startsAt:c.startsAt,expiresAt:c.endsAt||8640000000000000});return{};
  });
 }
 const postId=safe(data.postId),placement=(await db.doc(`promotionCampaigns/${postId}`).get()).data();
 if(!placement?.active||!placement.businessCampaignId)fail('failed-precondition','This promotion is no longer active.');
 const campaignRef=db.doc(`businessCampaigns/${placement.businessCampaignId}`),day=new Date(clock()).toISOString().slice(0,10);
 if(data.action==='prepareImpression'){
  // A short-lived ticket prevents replay and enforces a minimum display interval.
  const ticket=hash(uid+'|'+campaignRef.id+'|'+Math.floor(clock()/60000));
  await db.runTransaction(async tx=>{const c=(await tx.get(campaignRef)).data(),b=c?(await tx.get(db.doc(`businesses/${c.businessId}`))).data():null,previous=(await tx.get(db.doc(`billableImpressions/${hash(uid+'|'+campaignRef.id+'|'+day)}`))).exists,existing=(await tx.get(db.doc(`impressionTickets/${ticket}`))).exists;
   if(previous)fail('failed-precondition','This viewer has already been counted for today.');
   if(!eligible(c,clock())||!b?.verified||Object.hasOwn(b.members||{},uid))fail('failed-precondition','This view is not eligible for billing.');
   if(!existing)tx.create(db.doc(`impressionTickets/${ticket}`),{uid,postId,campaignId:campaignRef.id,createdAt:clock(),expiresAt:clock()+60000,deleteAfter:new Date(clock()+86400000)});});return{ticket};
 }
 const ticketRef=db.doc(`impressionTickets/${safe(data.ticket)}`),dedupRef=db.doc(`billableImpressions/${hash(uid+'|'+campaignRef.id+'|'+day)}`);
 return db.runTransaction(async tx=>{
  const [t,c,previous]=await Promise.all([tx.get(ticketRef),tx.get(campaignRef),tx.get(dedupRef)]);const ticket=t.data(),campaign=c.data();
  if(previous.exists)return{charged:false};
  if(!ticket||ticket.uid!==uid||ticket.postId!==postId||ticket.campaignId!==campaignRef.id||ticket.used||clock()<ticket.createdAt+1000||ticket.expiresAt<clock())fail('failed-precondition','The impression ticket expired or was already used.');
  if(!eligible(campaign,clock()))return{charged:false};
  const b=(await tx.get(db.doc(`businesses/${campaign.businessId}`))).data(),walletRef=db.doc(`businessWallets/${campaign.businessId}`),wallet=(await tx.get(walletRef)).data(),dailyRef=campaignRef.collection('days').doc(day),daily=(await tx.get(dailyRef)).data()||{},post=(await tx.get(db.doc(`feedPosts/${postId}`))).data(),live=(await tx.get(db.doc(`promotionCampaigns/${postId}`))).data();
  if(!b?.verified||Object.hasOwn(b.members||{},uid)||!post?.recipe||post.hidden||post.deleted||post.moderation?.status!=='approved'||!live?.active||live.businessCampaignId!==campaignRef.id)return{charged:false};
  if((wallet?.balanceMicro||0)<IMPRESSION_COST||(daily.spentMicro||0)+IMPRESSION_COST>campaign.dailyBudgetMinor*10000)return{charged:false};
  const spentMicro=(campaign.spentMicro||0)+IMPRESSION_COST;
  tx.update(walletRef,{balanceMicro:wallet.balanceMicro-IMPRESSION_COST});tx.update(campaignRef,{spentMicro,spentMinor:spentMicro/10000,impressions:(campaign.impressions||0)+1});
  tx.set(dailyRef,{spentMicro:(daily.spentMicro||0)+IMPRESSION_COST,impressions:(daily.impressions||0)+1});tx.update(ticketRef,{used:true});
  tx.create(dedupRef,{campaignId:campaignRef.id,day,costMicro:IMPRESSION_COST,createdAt:clock(),deleteAfter:new Date(clock()+90*86400000)});return{charged:true};
 });
};}
function safe(v){if(typeof v!=='string'||!/^[A-Za-z0-9_-]{1,100}$/.test(v))fail('invalid-argument','Invalid promotion identifier.');return v;}
module.exports={createCampaignLedger,eligible,CPM,IMPRESSION_COST};
