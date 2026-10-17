const {fail} = require('./domain');
const topics = {
  'Quick meals': ['quick', 'easy', 'weeknight', 'minute'],
  'Vegetarian': ['vegetarian', 'veggie', 'tofu', 'lentil', 'chickpea'],
  'Vegan': ['vegan', 'plant based'],
  'High protein': ['protein', 'chicken', 'turkey', 'tuna', 'eggs'],
  'Budget friendly': ['budget', 'cheap', 'affordable'],
  'Family meals': ['family', 'kids', 'children'],
  'Batch cooking': ['batch', 'meal prep', 'freezer', 'leftovers'],
  'Baking': ['baking', 'cake', 'bread', 'biscuit', 'cookie'],
  'Desserts': ['dessert', 'chocolate', 'pudding', 'sweet'],
  'Italian': ['italian', 'pasta', 'pizza', 'risotto'],
  'Indian': ['indian', 'curry', 'dal', 'masala'],
  'Mexican': ['mexican', 'taco', 'burrito', 'quesadilla'],
  'Asian': ['asian', 'thai', 'chinese', 'japanese', 'noodle', 'stir fry'],
  'Mediterranean': ['mediterranean', 'greek', 'hummus', 'falafel'],
  'Seafood': ['fish', 'seafood', 'salmon', 'prawn', 'tuna'],
  'Salads': ['salad', 'slaw']
};
const normalize = value => value.normalize('NFKD').replace(/\p{M}/gu, '').toLowerCase().replace(/[^\p{L}\p{N}]+/gu, ' ').trim();
function searchable(post) {
  return normalize([post.caption, post.authorName, post.recipe?.title, post.recipe?.description,
    ...(post.recipe?.ingredients || []).map(i => i.name)].filter(Boolean).join(' '));
}
function postTopics(post) {
  const content = searchable(post);
  return Object.keys(topics).filter(topic => topics[topic].some(term => new RegExp(`\\b${term}\\w*\\b`).test(content)));
}
function interests(value) {
  if (!Array.isArray(value) || value.length > 16 || value.some(t => typeof t !== 'string' || !Object.hasOwn(topics, t)))
    fail('invalid-argument', 'Choose food interests from the available options.');
  return [...new Set(value)];
}
function searchTerms(value = '') {
  if (typeof value !== 'string' || value.length > 120) fail('invalid-argument', 'Search with up to 120 characters.');
  return [...new Set(normalize(value).split(' ').filter(Boolean))].slice(0, 12);
}
function score(post, account) {
  return (Object.hasOwn(account.following || {}, post.authorId) ? 6 : 0) +
    postTopics(post).reduce((sum, topic) => sum + ((account.interests || []).includes(topic) ? 4 : 0) + Math.min(3, account.affinities?.[topic] || 0), 0);
}
function matches(post, terms) {
  const content = searchable(post) + ' ' + normalize(postTopics(post).join(' '));
  return !terms.length || terms.some(term => content.includes(term));
}
function ingredientMatches(name,excluded){return (' '+normalize(name||'')+' ').includes(' '+normalize(excluded)+' ');}
module.exports = {topics, interests, postTopics, searchable, searchTerms, score, matches,ingredientMatches};
