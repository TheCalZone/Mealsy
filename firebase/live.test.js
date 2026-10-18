// Read-only production smoke checks: no sign-in, account creation or data writes.
const {test} = require('node:test');
const assert = require('node:assert/strict');
test('deployed website preserves the social pause and invitation landing page',async()=>{
  const base='https://mealsy-b732b.web.app';
  const flag=await fetch(base+'/product-features.js',{cache:'no-store',signal:AbortSignal.timeout(30000)});
  assert.equal(flag.status,200);assert.match(await flag.text(),/socialEnabled\s*:\s*false/);
  for(const path of ['/join/','/share/']){const response=await fetch(base+path,{cache:'no-store',signal:AbortSignal.timeout(30000)});assert.equal(response.status,200);assert.match(response.headers.get('content-type'),/text\/html/);}
});

for (const [name, data] of [
  ['household', {action: 'read', scope: 'personal'}],
  ['householdMeals',{action:'capabilities'}],
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
