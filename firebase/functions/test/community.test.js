const {test} = require('node:test');
const assert = require('node:assert/strict');
const {database} = require('./feed-database');
const {createFeedHandler} = require('../feed');
const {decision, moderationRequest} = require('../moderation');
function fixture(check = async () => ({status: 'approved', provider: 'test'})) {
  const db = database(); let time=1000000;
  const handle = createFeedHandler(db, () => time, check);
  const call = (uid, data, token={email_verified:true}) => handle({auth:{uid,token},data});
  return {db,call,advance:()=>time+=11000};
}
const recipe={title:'Pasta',method:'Boil and serve',servings:2,prepMinutes:10,ingredients:[]};
const post={action:'publish',id:'recipe1',caption:'Italian pasta',recipe,photo:'',authorName:'Forged name'};
test('usernames get server-assigned disambiguators and cannot be impersonated in a post or comment',async()=>{
  const f=fixture();
  const [a,b]=await Promise.all(['alice','bob'].map(uid=>f.call(uid,{action:'setUsername',username:'Home Cook'})));
  assert.notEqual(a.discriminator,b.discriminator);assert.equal(a.username,b.username);
  assert.deepEqual(await f.call('alice',{action:'setUsername',username:'Home Cook'}),a);
  await f.call('alice',post);await f.call('bob',{action:'comment',postId:'recipe1',id:'comment1',text:'Lovely',authorName:'Someone else'});
  const page=await f.call('bob',{action:'list'});assert.equal(page.posts[0].authorName,'Home Cook');assert(!('discriminator'in page.posts[0]));
  assert.equal((await f.call('bob',{action:'comments',postId:'recipe1'})).comments[0].authorName,'Home Cook');
  await assert.rejects(f.call('alice',{action:'setUsername',username:'\n'}),{code:'invalid-argument'});
});
test('email confirmation is enforced on the server and cannot be supplied in data',async()=>{
  const f=fixture();await f.call('alice',{action:'setUsername',username:'Alice'});
  await assert.rejects(f.call('alice',{...post,emailVerified:true},{}),{code:'failed-precondition'});
  assert(!f.db.docs.has('feedPosts/recipe1'));
  assert.equal((await f.call('alice',{action:'profile'},{})).emailVerified,false);
});
test('automatic safety rejection or outage prevents public writes; retries remain safe',async()=>{
  let reject=false,calls=0;
  const f=fixture(async content=>{calls++;if(reject){const error=Error('Safety unavailable');error.code='unavailable';throw error;}return{status:'approved',provider:'test'};});
  await f.call('alice',{action:'setUsername',username:'Alice'});reject=true;
  await assert.rejects(f.call('alice',post),{code:'unavailable'});assert(!f.db.docs.has('feedPosts/recipe1'));
  reject=false;await f.call('alice',post);const before=calls;await f.call('alice',post);assert.equal(calls,before);
  reject=true;await assert.rejects(f.call('alice',{action:'comment',postId:'recipe1',id:'c1',text:'Comment'}),{code:'unavailable'});
  assert(!f.db.docs.has('feedPosts/recipe1/comments/c1'));
  const moderation=f.db.docs.get('feedPosts/recipe1').moderation;assert.equal(moderation.status,'approved');
});
test('search ignores the selected feed, including stale modes from old clients',async()=>{
  const f=fixture();await f.call('alice',{action:'setUsername',username:'Alice'});await f.call('alice',post);
  for(const mode of ['following','undefined-mode'])assert.equal((await f.call('bob',{action:'list',mode,search:'pasta'})).posts.length,1);
});
test('promotions need independent payment verification, are labelled, expire and cannot bypass visibility',async()=>{
  const f=fixture();await f.call('alice',{action:'setUsername',username:'Alice'});await f.call('alice',post);
  const requested=await f.call('alice',{action:'requestPromotion',postId:'recipe1',paid:true});assert.equal(requested.checkoutEnabled,false);
  await assert.rejects(f.call('alice',{action:'activatePromotion',postId:'recipe1',receiptId:'receipt',paid:true}),{code:'permission-denied'});
  const admin={promotionAdmin:true,email_verified:true};
  await assert.rejects(f.call('admin',{action:'activatePromotion',postId:'recipe1',receiptId:'receipt'},admin),{code:'failed-precondition'});
  f.db.docs.set('promotionReceipts/receipt',{verified:true,owner:'alice',postId:'recipe1',expiresAt:2000000});
  await f.call('admin',{action:'activatePromotion',postId:'recipe1',receiptId:'receipt'},admin);
  assert.equal((await f.call('bob',{action:'list',mode:'forYou'})).posts[0].promoted,true);
  f.db.docs.get('promotionReceipts/receipt').revoked=true;
  assert.equal((await f.call('bob',{action:'list'})).posts[0].promoted,false);
  f.db.docs.get('feedPosts/recipe1').hidden=true;assert.equal((await f.call('bob',{action:'list'})).posts.length,0);
});
test('moderation includes full text plus photos and fails closed for malformed provider responses',()=>{
  const request=moderationRequest({caption:'dinner',recipe:{method:'cook'},photo:'YWJj'});
  assert.equal(request.contents[0].parts.length,2);assert(request.systemInstruction.parts[0].text.includes('untrusted'));
  assert.equal(decision({candidates:[{finishReason:'STOP',content:{parts:[{text:'{"allowed":true}'}]}}]}),true);
  assert.equal(decision({promptFeedback:{blockReason:'SAFETY'}}),false);
  assert.equal(decision({candidates:[{finishReason:'STOP',content:{parts:[{text:'{"allowed":false}'}]}}]}),false);
  for(const value of [{},{candidates:[{finishReason:'STOP',content:{parts:[{text:'not json'}]}}]}])assert.throws(()=>decision(value),{code:'unavailable'});
});
