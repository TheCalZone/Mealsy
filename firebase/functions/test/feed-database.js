const assert = require('node:assert/strict');
// Deterministic serial transaction double, including query cursors. The emulator
// suite separately exercises the production Firestore SDK and rules.
function database() {
  const docs = new Map(); let queue = Promise.resolve();
  function snapshot(path) {
    const data = structuredClone(docs.get(path));
    return {id: path.split('/').at(-1), ref: ref(path), exists: data !== undefined, data: () => data};
  }
  const ref = path => ({path, id: path.split('/').at(-1), collection: name => collection(`${path}/${name}`), get: async () => snapshot(path)});
  function collection(path, sort = [], filters = [], start = null, max = Infinity) {
    const result = {_query: true, doc: id => ref(`${path}/${id}`),
      orderBy: (field, direction) => collection(path, [...sort, [typeof field === 'string' ? field : '__name__', direction]], filters, start, max),
      where: (field, op, value) => { assert.equal(op, '=='); return collection(path, sort, [...filters, [field, value]], start, max); },
      startAfter: (...values) => collection(path, sort, filters, values, max),
      limit: n => collection(path, sort, filters, start, n),
      get: async () => {
        const field = (doc, key) => key === '__name__' ? doc.id : doc.data()[key];
        const compare = (a, b) => a < b ? -1 : a > b ? 1 : 0;
        let items = [...docs.keys()].filter(key => key.startsWith(path + '/') && key.split('/').length === path.split('/').length + 1).map(snapshot);
        items = items.filter(doc => filters.every(([key, value]) => doc.data()[key] === value));
        items.sort((a, b) => { for (const [key, direction] of sort) { const c = compare(field(a, key), field(b, key)) * (direction === 'desc' ? -1 : 1); if (c) return c; } return 0; });
        if (start) items = items.filter(doc => { for (let i = 0; i < sort.length; i++) { const c = compare(field(doc, sort[i][0]), start[i]) * (sort[i][1] === 'desc' ? -1 : 1); if (c) return c > 0; } return false; });
        items = items.slice(0, max); return {docs: items, size: items.length};
      }
    }; return result;
  }
  return {docs, doc: ref, collection, runTransaction(action) {
    const run = queue.then(async () => {
      const staged = new Map();
      const tx = {
        get: async r => { assert.equal(staged.size, 0, 'All Firestore reads must precede writes'); return r._query ? r.get() : snapshot(r.path); },
        set: (r, value) => staged.set(r.path, structuredClone(value)),
        create: (r, value) => { assert(!docs.has(r.path)); staged.set(r.path, structuredClone(value)); },
        update: (r, value) => { assert(docs.has(r.path)); staged.set(r.path, {...docs.get(r.path), ...structuredClone(value)}); },
        delete: r => staged.set(r.path, undefined)
      };
      const result = await action(tx);
      for (const [key, value] of staged) value === undefined ? docs.delete(key) : docs.set(key, value);
      return result;
    }); queue = run.catch(() => {}); return run;
  }};
}
module.exports = {database};
