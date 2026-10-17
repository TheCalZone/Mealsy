const {test} = require('node:test');
const assert = require('node:assert/strict');
const {createHandler} = require('../application');
const {createFeedHandler} = require('../feed');
const {createBilling} = require('../billing');
const {applyChanges} = require('../domain');

// Validation must finish before a malformed request can consume database reads.
const db = new Proxy({}, {get(_, key) {
  if (key === 'doc') return () => ({}); // Constructing a reference performs no I/O.
  throw Error('Unexpected database access');
}});
const handlers = [createHandler(db), createFeedHandler(db), createBilling(db).handle];
for (const [index, handle] of handlers.entries()) {
  test(`callable ${index} rejects malformed and unsupported actions before database access`, async () => {
    for (const data of [undefined, null, [], 'status', 1, false, {}, {action: null}, {action: 'unknown'}])
      await assert.rejects(handle({auth: {uid: 'owner', token: {}}, data}), {code: 'invalid-argument'});
  });
  test(`callable ${index} checks authentication before payload validation`, async () => {
    await assert.rejects(handle({data: null}), {code: 'unauthenticated'});
  });
}

test('household names, invitation codes and list scopes require the expected types', async () => {
  const handle = createHandler(db), auth = {uid: 'owner', token: {}};
  for (const data of [{action: 'create', name: {}}, {action: 'join', code: []},
    {action: 'read'}, {action: 'patch', scope: 'another-user'}])
    await assert.rejects(handle({auth, data}), {code: 'invalid-argument'});
});

test('malformed shopping changes return validation errors without mutating the list', () => {
  const milk = {Id: '11111111-1111-1111-1111-111111111111', Name: 'Milk', Category: 'Dairy', Quantity: '', Note: '', InBasket: false};
  const current = {[milk.Id]: milk};
  for (const change of [null, undefined, [], 'remove', 42, true, {}]) {
    assert.throws(() => applyChanges(current, [change]), {code: 'invalid-argument'});
    assert.deepEqual(current, {[milk.Id]: milk});
  }
});
