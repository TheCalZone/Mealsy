const {onCall, HttpsError} = require('firebase-functions/v2/https');
const {initializeApp} = require('firebase-admin/app');
const {getFirestore} = require('firebase-admin/firestore');
const {DomainError} = require('./domain');
const {createHandler} = require('./application');
initializeApp();
const handle = createHandler(getFirestore());

exports.household = onCall({region: 'us-central1', maxInstances: 10, timeoutSeconds: 30}, async request => {
  try { return await handle(request); }
  catch (error) {
    if (error instanceof DomainError) throw new HttpsError(error.code, error.message);
    console.error('Household operation failed', error.code || error.name);
    throw new HttpsError('internal', 'Could not complete the request. Please try again.');
  }
});

const feed = require('./feed').createFeedHandler(getFirestore());
exports.foodFeed = onCall({region: 'us-central1', maxInstances: 10, timeoutSeconds: 30}, async request => {
  try { return await feed(request); }
  catch (error) {
    if (error instanceof DomainError) throw new HttpsError(error.code, error.message);
    console.error('Feed operation failed', error.code || error.name);
    throw new HttpsError('internal', 'The food feed is unavailable. Please try again.');
  }
});

const billing = require('./billing').createBilling(getFirestore());
exports.billing = onCall({region: 'us-central1', maxInstances: 10, timeoutSeconds: 60}, async request => {
  try { return await billing.handle(request); }
  catch (error) {
    if (error instanceof DomainError) throw new HttpsError(error.code, error.message);
    console.error('Billing verification failed', error.code || error.name);
    throw new HttpsError('unavailable', 'Your plan could not be verified. Please try restoring purchases when connected.');
  }
});
const {onMessagePublished} = require('firebase-functions/v2/pubsub');
exports.playSubscriptionUpdates = onMessagePublished({topic: 'mealsy-play-billing', region: 'us-central1', retry: true}, async event => {
  const data = event.data.message.json;
  if (data?.packageName === require('./billing').PACKAGE && data.subscriptionNotification?.purchaseToken)
    await billing.notification(data.subscriptionNotification.purchaseToken);
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
