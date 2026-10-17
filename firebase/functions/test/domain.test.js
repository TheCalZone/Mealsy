const {test} = require('node:test');
const assert = require('node:assert/strict');
const {applyChanges, requireMember, requireOwner} = require('../domain');
const milk = {Id: '11111111-1111-1111-1111-111111111111', Name: 'Milk', Category: 'Dairy', Quantity: '1 L', Note: '', InBasket: false};
const rice = {...milk, Id: '22222222-2222-2222-2222-222222222222', Name: 'Rice', Category: 'Pantry'};
const wire = item => Object.fromEntries(Object.entries(item).map(([key, value]) => [key[0].toLowerCase() + key.slice(1), value]));

test('Android camelCase bread changes support add, retry, edit and delete against stored PascalCase', () => {
  const bread = {...milk, Name: 'Bread', Category: 'Bakery', Quantity: ''};
  const addition = [{before: null, after: wire(bread)}];
  const stored = applyChanges({}, addition);
  assert.deepEqual(stored[bread.Id], bread);
  assert.deepEqual(applyChanges(stored, addition), stored);
  const collected = {...bread, InBasket: true};
  const updated = applyChanges(stored, [{before: wire(bread), after: wire(collected)}]);
  assert.deepEqual(updated[bread.Id], collected);
  assert.deepEqual(applyChanges(updated, [{before: wire(collected), after: null}]), {});
});

test('camelCase payloads preserve validation and conflict detection', () => {
  assert.throws(() => applyChanges({}, [{after: {...wire(milk), id: 'bad'}}]), {code: 'invalid-argument'});
  assert.throws(() => applyChanges({}, [{after: {...wire(milk), inBasket: 'true'}}]), {code: 'invalid-argument'});
  assert.throws(() => applyChanges({}, [{after: {...wire(milk), Id: rice.Id}}]), {code: 'invalid-argument'});
  assert.throws(() => applyChanges({[milk.Id]: {...milk, Quantity: '3 L'}}, [{before: wire(milk), after: wire({...milk, Quantity: '2 L'})}]), {code: 'aborted'});
});
test('concurrent additions preserve both members items', () => {
  const first = applyChanges({}, [{before: null, after: milk}]);
  const second = applyChanges(first, [{before: null, after: rice}]);
  assert.deepEqual(Object.values(second), [milk, rice]);
});
test('independent fields on one item merge', () => {
  const first = applyChanges({[milk.Id]: milk}, [{before: milk, after: {...milk, InBasket: true}}]);
  const result = applyChanges(first, [{before: milk, after: {...milk, Quantity: '2 L'}}]);
  assert.deepEqual(result[milk.Id], {...milk, InBasket: true, Quantity: '2 L'});
});
test('competing edits reject without mutating server state', () => {
  const first = {[milk.Id]: {...milk, Quantity: '2 L'}};
  assert.throws(() => applyChanges(first, [{before: milk, after: {...milk, Quantity: '3 L'}}]), {code: 'aborted'});
  assert.equal(first[milk.Id].Quantity, '2 L');
});
test('stale delete cannot remove another members edit', () => {
  assert.throws(() => applyChanges({[milk.Id]: {...milk, Note: 'Oat'}}, [{before: milk, after: null}]), {code: 'aborted'});
});
test('stale edit cannot resurrect removed items', () => {
  assert.throws(() => applyChanges({}, [{before: milk, after: {...milk, InBasket: true}}]), {code: 'aborted'});
});
test('retry after lost responses is idempotent for add/edit/delete', () => {
  for (const [current, change] of [[{[milk.Id]: milk}, {before: null, after: milk}],
    [{[milk.Id]: {...milk, InBasket: true}}, {before: milk, after: {...milk, InBasket: true}}],
    [{}, {before: milk, after: null}]]) assert.deepEqual(applyChanges(current, [change]), current);
});
test('invalid and oversized writes are rejected', () => {
  assert.throws(() => applyChanges({}, [{after: {...milk, Name: 'x'.repeat(301)}}]), {code: 'invalid-argument'});
  assert.throws(() => applyChanges({}, [{after: {...milk, InBasket: 'true'}}]), {code: 'invalid-argument'});
  assert.throws(() => applyChanges({}, Array(501).fill({after: milk})), {code: 'invalid-argument'});
});
test('member operations and owner operations enforce roles', () => {
  const house = {owner: 'owner', members: {owner: 'Parent', child: 'Child'}};
  requireMember(house, 'child'); requireOwner(house, 'owner');
  assert.throws(() => requireMember(house, 'stranger'), {code: 'permission-denied'});
  assert.throws(() => requireOwner(house, 'child'), {code: 'permission-denied'});
  assert.throws(() => requireMember(null, 'owner'), {code: 'permission-denied'});
  assert.throws(() => requireMember(house, 'toString'), {code: 'permission-denied'});
});
test('server rejects identifiers the Android client cannot deserialize', () => {
  for (const Id of ['------------------------------------', '00000000-0000-0000-0000-000000000000', 'not-a-guid'])
    assert.throws(() => applyChanges({}, [{after: {...milk, Id}}]), {code: 'invalid-argument'});
});
test('server canonicalizes names and category aliases like the Android client', () => {
  const result = applyChanges({}, [{after: {...milk, Name: ' Milk ', Category: ' vegetables '}}]);
  assert.equal(result[milk.Id].Name, 'Milk'); assert.equal(result[milk.Id].Category, 'Produce');
});
