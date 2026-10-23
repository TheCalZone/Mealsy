const {onCall, HttpsError} = require('firebase-functions/v2/https');
const {initializeApp} = require('firebase-admin/app');
const {getFirestore} = require('firebase-admin/firestore');
const {DomainError} = require('./domain');
const {createHandler} = require('./application');
initializeApp();
const handle = createHandler(getFirestore());
const {protect:protectCall}=require('./callable-errors');
// Disabled/deleting users can still hold an unexpired ID token. Block all
// callables using the durable deletion marker, including old app versions.
function protect(handler,service){return protectCall(async request=>{
 if(service!=='accountDeletion'&&request.auth&&(await getFirestore().doc(`accountDeletions/${request.auth.uid}`).get()).exists)
   require('./domain').fail('permission-denied','This account is being deleted.');
 return handler(request);
},service);}
const accountDeletion=require('./account-deletion').createAccountDeletion(getFirestore(),require('firebase-admin/auth').getAuth());
exports.accountDeletion=onCall({region:'us-central1',maxInstances:3,timeoutSeconds:30},protect(accountDeletion.handle,'accountDeletion'));
exports.processAccountDeletion=require('firebase-functions/v2/firestore').onDocumentCreated({document:'accountDeletions/{uid}',region:'europe-west1',retry:true,maxInstances:2,timeoutSeconds:540},event=>accountDeletion.process(event.params.uid));
const {socialHandler,socialEnabled}=require('./product-features');
exports.householdMeals=onCall({region:'us-central1',maxInstances:5,timeoutSeconds:30},protect(require('./household-meals').createHouseholdMeals(getFirestore(),require('firebase-admin/auth').getAuth()),'householdMeals'));
const business=require('./business').createBusiness(getFirestore(),require('firebase-admin/auth').getAuth());
exports.business=onCall({region:'us-central1',maxInstances:10,timeoutSeconds:60},protect(socialHandler(business),'business'));
exports.notifications=onCall({region:'us-central1',maxInstances:5,timeoutSeconds:30},protect(require('./notifications').createNotifications(getFirestore(),{socialEnabled}),'notifications'));
exports.promotions=onCall({region:'us-central1',maxInstances:5,timeoutSeconds:30},protect(socialHandler(require('./campaign-ledger').createCampaignLedger(getFirestore())),'promotions'));
const {onDocumentCreated,onDocumentDeleted}=require('firebase-functions/v2/firestore');
exports.cleanupHousehold=onDocumentDeleted({document:'households/{householdId}',region:'europe-west1',retry:true,maxInstances:3},async event=>{
  await getFirestore().recursiveDelete(getFirestore().doc(`households/${event.params.householdId}`));
});
exports.householdQuestions=onCall({region:'us-central1',maxInstances:5,timeoutSeconds:30},protect(require('./shopping-questions').createShoppingQuestions(getFirestore()),'householdQuestions'));
exports.deliverActivity=onDocumentCreated({document:'notifications/{uid}/items/{itemId}',region:'europe-west1',retry:true,maxInstances:5},async event=>{
  if(!event.data||(!socialEnabled&&event.data.data().type!=='householdQuestion')||Date.now()-(event.data.data().createdAt||0)>3600000)return;
  if(event.data.data().type==='householdQuestion'){const n=event.data.data(),home=(await getFirestore().doc(`households/${n.householdId}`).get()).data();if(!Object.hasOwn(home?.members||{},event.params.uid))return;} // Paused/stale alerts do not query devices or send push.
  const db=getFirestore(),devices=await db.collection('notificationDevices').where('uid','==',event.params.uid).limit(10).get();
  await Promise.all(devices.docs.map(async doc=>{
    const device=doc.data();if(Date.now()-device.updatedAt>90*86400000)return;
    try{await require('firebase-admin/messaging').getMessaging().send({token:device.token,data:{recipient:event.params.uid,eventId:event.params.itemId,type:event.data.data().type==='householdQuestion'?'householdQuestion':'activity'},android:{priority:'high',ttl:3600000,collapseKey:'mealsy-activity'}});}
    catch(error){if(['messaging/registration-token-not-registered','messaging/invalid-registration-token'].includes(error.code))await db.runTransaction(async tx=>{const current=(await tx.get(doc.ref)).data();if(current?.uid===device.uid&&current?.updatedAt===device.updatedAt)tx.delete(doc.ref);});else throw new Error('Activity delivery temporarily unavailable.');}
  }));
});

exports.household = onCall({region: 'us-central1', maxInstances: 10, timeoutSeconds: 30}, protect(handle,'household'));

const feed = require('./feed').createFeedHandler(getFirestore());
const administration = require('./administration').createAdministration(getFirestore(), require('firebase-admin/auth').getAuth());
exports.administration = onCall({region:'us-central1',maxInstances:5,timeoutSeconds:60},protect(administration,'administration'));
exports.foodFeed = onCall({region: 'us-central1', maxInstances: 10, timeoutSeconds: 60},protect(socialHandler(feed),'foodFeed'));

const billing = require('./billing').createBilling(getFirestore());
exports.billing = onCall({region: 'us-central1', maxInstances: 10, timeoutSeconds: 60},protect(billing.handle,'billing'));
const {onMessagePublished} = require('firebase-functions/v2/pubsub');
exports.playSubscriptionUpdates = onMessagePublished({topic: 'mealsy-play-billing', region: 'us-central1', retry: true}, async event => {
  const data = event.data.message.json;
  if (data?.packageName === require('./billing').PACKAGE && data.subscriptionNotification?.purchaseToken)
    await billing.notification(data.subscriptionNotification.purchaseToken);
  if(data?.packageName===require('./billing').PACKAGE&&data.oneTimeProductNotification?.sku===require('./promotion-purchases').PRODUCT)
    await require('./promotion-purchases').createPromotionPurchases(getFirestore()).notification(data.oneTimeProductNotification.purchaseToken);
  if(data?.packageName===require('./billing').PACKAGE&&data.voidedPurchaseNotification?.purchaseToken){
    await require('./promotion-purchases').createPromotionPurchases(getFirestore()).notification(data.voidedPurchaseNotification.purchaseToken);
    await billing.notification(data.voidedPurchaseNotification.purchaseToken);
  }
});
const {onSchedule} = require('firebase-functions/v2/scheduler');
exports.reconcileSubscriptions = onSchedule({schedule: 'every 60 minutes', region: 'us-central1', timeoutSeconds: 540}, async () => {
  let cursor;
  do {
    let query = getFirestore().collection('billingPurchases').where('expiresAt', '>', Date.now()).orderBy('expiresAt').limit(100);
    if (cursor) query = query.startAfter(cursor);
    const page = await query.get();
    for (let i = 0; i < page.docs.length; i += 5)
      await Promise.all(page.docs.slice(i, i + 5).map(doc => billing.notification(doc.data().token)));
    cursor = page.size === 100 ? page.docs.at(-1) : null;
  } while (cursor);
});

// Recovery also covers process termination and the second, post-request sweep.
exports.resumeAccountDeletions=onSchedule({schedule:'every 5 minutes',region:'us-central1',maxInstances:1,timeoutSeconds:540},async()=>{
 const jobs=await getFirestore().collection('accountDeletions').where('status','in',['queued','retry','cleaning','settling']).where('retryAfter','<=',Date.now()).orderBy('retryAfter').limit(5).get();
 for(const job of jobs.docs){try{await accountDeletion.process(job.id);}catch{console.error('Account deletion will retry',{reference:job.data().reference});}}
});
