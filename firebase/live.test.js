// Read-only production smoke checks: no sign-in, account creation or data writes.
const {test} = require('node:test');
const assert = require('node:assert/strict');

for (const [name, data] of [
  ['household', {action: 'read', scope: 'personal'}],
  ['foodFeed', {action: 'list'}],
  ['billing', {action: 'status'}],
  ['administration', {action: 'status'}],
  ['business',{action:'mine'}],
  ['notifications',{action:'list'}],
  ['promotions',{action:'wallet',businessId:'none'}]
]) {
  test(`deployed ${name} requires Firebase authentication`, async () => {
    const response = await fetch(`https://us-central1-mealsy-b732b.cloudfunctions.net/${name}`, {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify({data}),
      signal: AbortSignal.timeout(60000)
    });
    assert.equal(response.status, 401, `${name} is missing, unavailable, or accepts unsigned requests`);
    assert.equal((await response.json()).error.status, 'UNAUTHENTICATED');
  });
}
