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
