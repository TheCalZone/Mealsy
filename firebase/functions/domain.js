const {isDeepStrictEqual} = require('node:util');
class DomainError extends Error {
  constructor(code, message, details) { super(message); this.code = code; this.details = details; }
}
const fail = (code, message, details) => { throw new DomainError(code, message, details); };
const fields = ['Name', 'Category', 'Quantity', 'Note', 'InBasket'];
function item(value) {
  // HttpClient's JsonContent uses camelCase, while stored lists and older clients
  // use PascalCase. Normalize both before validation and three-way comparison.
  if (value && typeof value === 'object' && !Array.isArray(value)) {
    const normalized = {};
    for (const field of ['Id', ...fields]) {
      const camel = field[0].toLowerCase() + field.slice(1);
      if (Object.hasOwn(value, field) && Object.hasOwn(value, camel) && value[field] !== value[camel])
        fail('invalid-argument', 'Conflicting shopping item fields.');
      normalized[field] = Object.hasOwn(value, field) ? value[field] : value[camel];
    }
    value = normalized;
  }
  if (!value || typeof value !== 'object' || !/^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$/i.test(value.Id || '') || value.Id === '00000000-0000-0000-0000-000000000000')
    fail('invalid-argument', 'Invalid shopping item.');
  const clean = {Id: value.Id.toLowerCase()};
  for (const field of fields) {
    const v = value[field];
    if (field === 'InBasket') {
      if (typeof v !== 'boolean') fail('invalid-argument', 'Invalid collected status.');
    } else if (typeof v !== 'string' || v.length > (field === 'Note' ? 2000 : 300))
      fail('invalid-argument', 'Item text is too long or invalid.');
    clean[field] = v;
  }
  if (!clean.Name.trim()) fail('invalid-argument', 'An item needs a name.');
  clean.Name = clean.Name.trim();
  const categories = {'': 'Other', '.': 'Other', other: 'Other', veg: 'Produce', vegetables: 'Produce', fruits: 'Produce', produce: 'Produce',
    tins: 'Pantry', 'tins & cans': 'Pantry', pantry: 'Pantry', beverages: 'Drinks', drinks: 'Drinks', meat: 'Meat', 'meat & poultry': 'Meat',
    dairy: 'Dairy', bakery: 'Bakery', frozen: 'Frozen'};
  clean.Category = Object.hasOwn(categories, clean.Category.trim().toLowerCase()) ? categories[clean.Category.trim().toLowerCase()] : clean.Category.trim();
  return clean;
}
// Three-way field merge: independent fields/items merge; competing edits never silently win.
function applyChanges(current, changes) {
  if (!Array.isArray(changes) || changes.length > 500) fail('invalid-argument', 'Too many changes.');
  const result = structuredClone(current || {});
  const seen = new Set();
  for (const change of changes) {
    if (!change || typeof change !== 'object' || Array.isArray(change))
      fail('invalid-argument', 'Invalid change.');
    const before = change.before == null ? null : item(change.before);
    const after = change.after == null ? null : item(change.after);
    const id = after?.Id || before?.Id;
    if (!id || (before && after && before.Id !== after.Id) || seen.has(id))
      fail('invalid-argument', 'Invalid change.');
    seen.add(id);
    const remote = result[id] || null;
    if (isDeepStrictEqual(remote, after)) continue; // Safe retry after a lost response.
    if (!before) {
      if (remote) fail('aborted', 'An item changed on another device.');
      result[id] = after;
    } else if (!after) {
      if (!remote) continue;
      if (!isDeepStrictEqual(remote, before)) fail('aborted', 'An item was edited before it could be removed.');
      delete result[id];
    } else {
      if (!remote) fail('aborted', 'An item was removed on another device.');
      const merged = {...remote};
      for (const field of fields) if (after[field] !== before[field]) {
        if (remote[field] !== before[field] && remote[field] !== after[field])
          fail('aborted', 'The same item detail was edited on another device.');
        merged[field] = after[field];
      }
      result[id] = merged;
    }
  }
  if (Object.keys(result).length > 500 || Buffer.byteLength(JSON.stringify(result)) > 450000)
    fail('resource-exhausted', 'Lists support up to 500 items and 450 KB.');
  return result;
}
function requireMember(household, uid) {
  if (!household || !Object.hasOwn(household.members || {}, uid)) fail('permission-denied', 'You no longer belong to this household.');
}
function requireOwner(household, uid) {
  requireMember(household, uid);
  if (household.owner !== uid) fail('permission-denied', 'Only the household owner can do this.');
}
module.exports = {DomainError, fail, applyChanges, requireMember, requireOwner};
