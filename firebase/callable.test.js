const {test} = require('node:test');
const assert = require('node:assert/strict');
const endpoint = 'http://127.0.0.1:5001/demo-mealsy/us-central1/household';
const post = async (data, token) => {
  const response = await fetch(endpoint, {method: 'POST', headers: {'Content-Type': 'application/json', ...(token ? {Authorization: `Bearer ${token}`} : {})}, body: JSON.stringify({data})});
  return {status: response.status, body: await response.json()};
};
test('callable HTTP protocol rejects unauthenticated list requests', async () => {
  const result = await post({action: 'read', scope: 'personal'});
  assert.equal(result.status, 401); assert.equal(result.body.error.status, 'UNAUTHENTICATED');
});
test('callable accepts an Auth emulator token and returns domain validation errors', async () => {
  assert(process.env.FIREBASE_AUTH_EMULATOR_HOST, 'Auth emulator is required');
  const response = await fetch(`http://${process.env.FIREBASE_AUTH_EMULATOR_HOST}/identitytoolkit.googleapis.com/v1/accounts:signUp?key=demo-key`, {
    method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({email: `test-${Date.now()}@example.com`, password: 'Local-test-only-123!', returnSecureToken: true})
  });
  const account = await response.json(); assert(account.idToken);
  // Invalid name is rejected before any Firestore operation, so no database is contacted.
  const result = await post({action: 'create', name: ''}, account.idToken);
  assert.equal(result.status, 400); assert.equal(result.body.error.status, 'INVALID_ARGUMENT');
});
test('food feed callable rejects unsigned requests before database access', async () => {
  const response = await fetch(endpoint.replace('/household', '/foodFeed'), {method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({data: {action: 'list'}})});
  assert.equal(response.status, 401); assert.equal((await response.json()).error.status, 'UNAUTHENTICATED');
});
test('authenticated food publication validates public content before accessing Firestore', async () => {
  const response = await fetch(`http://${process.env.FIREBASE_AUTH_EMULATOR_HOST}/identitytoolkit.googleapis.com/v1/accounts:signUp?key=demo-key`, {
    method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({email: `feed-${Date.now()}@example.com`, password: 'Local-test-only-123!', returnSecureToken: true})
  });
  const account = await response.json(); assert(account.idToken);
  const result = await fetch(endpoint.replace('/household', '/foodFeed'), {method: 'POST', headers: {'Content-Type': 'application/json', Authorization: `Bearer ${account.idToken}`}, body: JSON.stringify({data: {action: 'publish', id: 'test', caption: '', authorName: '', recipe: null, photo: ''}})});
  assert.equal(result.status, 400); assert.equal((await result.json()).error.status, 'INVALID_ARGUMENT');
});
test('billing callable rejects unsigned purchase verification requests', async () => {
  const response = await fetch(endpoint.replace('/household', '/billing'), {method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({data: {action: 'verify', token: 'untrusted-token'}})});
  assert.equal(response.status, 401); assert.equal((await response.json()).error.status, 'UNAUTHENTICATED');
});
