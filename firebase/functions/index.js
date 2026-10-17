const {onCall, HttpsError} = require('firebase-functions/v2/https');
const {initializeApp} = require('firebase-admin/app');
const {getFirestore} = require('firebase-admin/firestore');
const {DomainError} = require('./domain');
const {createHandler} = require('./application');
initializeApp();
const handle = createHandler(getFirestore());
const {protect}=require('./callable-errors');
const business=require('./business').createBusiness(getFirestore(),require('firebase-admin/auth').getAuth());
exports.business=onCall({region:'us-central1',maxInstances:10,timeoutSeconds:60},protect(business,'business'));
exports.notifications=onCall({region:'us-central1',maxInstances:5,timeoutSeconds:30},protect(require('./notifications').createNotifications(getFirestore()),'notifications'));
exports.promotions=onCall({region:'us-central1',maxInstances:5,timeoutSeconds:30},protect(require('./campaign-ledger').createCampaignLedger(getFirestore()),'promotions'));
const {onDocumentCreated}=require('firebase-functions/v2/firestore');
exports.deliverActivity=onDocumentCreated({document:'notifications/{uid}/items/{itemId}',region:'europe-west1',retry:true,maxInstances:5},async event=>{
  if(!event.data||Date.now()-(event.data.data().createdAt||0)>3600000)return; // Stale alerts remain in the inbox; stop push retries after an hour.
  const db=getFirestore(),devices=await db.collection('notificationDevices').where('uid','==',event.params.uid).limit(10).get();
  await Promise.all(devices.docs.map(async doc=>{
    const device=doc.data();if(Date.now()-device.updatedAt>90*86400000)return;
    try{await require('firebase-admin/messaging').getMessaging().send({token:device.token,data:{recipient:event.params.uid,eventId:event.params.itemId},android:{priority:'high',ttl:3600000,collapseKey:'mealsy-activity'}});}
    catch(error){if(['messaging/registration-token-not-registered','messaging/invalid-registration-token'].includes(error.code))await db.runTransaction(async tx=>{const current=(await tx.get(doc.ref)).data();if(current?.uid===device.uid&&current?.updatedAt===device.updatedAt)tx.delete(doc.ref);});else throw new Error('Activity delivery temporarily unavailable.');}
  }));
});

exports.household = onCall({region: 'us-central1', maxInstances: 10, timeoutSeconds: 30}, protect(handle,'household'));

const feed = require('./feed').createFeedHandler(getFirestore());
const administration = require('./administration').createAdministration(getFirestore(), require('firebase-admin/auth').getAuth());
exports.administration = onCall({region:'us-central1',maxInstances:5,timeoutSeconds:60},protect(administration,'administration'));
exports.foodFeed = onCall({region: 'us-central1', maxInstances: 10, timeoutSeconds: 60},protect(feed,'foodFeed'));

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
