const {test}=require('node:test');
const assert=require('node:assert/strict');
const {database}=require('./feed-database');
const {createAdministration}=require('../administration');
function fixture() {
  const db=database();
  const users={owner:{uid:'owner',email:'owner@example.com',customClaims:{admin:true}},member:{uid:'member',email:'member@example.com',emailVerified:true,customClaims:{}}};
  const auth={getUser:async uid=>{if(!users[uid])throw Error('No user');return users[uid];},getUserByEmail:async email=>{
    const user=Object.values(users).find(u=>u.email===email);if(user)return user;throw Object.assign(Error('Missing'),{code:'auth/user-not-found'});
  }};
  const handle=createAdministration(db,auth,()=>1000000);
  const call=(uid,data,token={})=>handle({auth:uid?{uid,token}:null,data});
  db.docs.set('feedPosts/post',{authorId:'member',authorName:'Cook',caption:'Soup',createdAt:10,hidden:false,moderation:{status:'approved'}});
  return{db,users,call};
}
test('administration requires a current server-owned admin role, not profile flags or claimed token data',async()=>{
  const f=fixture();await assert.rejects(f.call(null,{action:'status'}),{code:'unauthenticated'});
  f.db.docs.set('users/member',{admin:true});
  assert.deepEqual(await f.call('member',{action:'status'},{admin:true}),{admin:false});
  for(const action of ['lookupUser','posts','setFamilyGrant','setPostHidden','resolveReport','pausePromotion','audit'])
    await assert.rejects(f.call('member',{action,admin:true},{admin:true}),{code:'permission-denied'});
  assert.deepEqual(await f.call('owner',{action:'status'}),{admin:true});
  f.users.owner.customClaims.admin=false;
  await assert.rejects(f.call('owner',{action:'posts'},{admin:true}),{code:'permission-denied'});
  f.users.owner.customClaims.admin=true;f.users.owner.disabled=true;
  await assert.rejects(f.call('owner',{action:'posts'}),{code:'permission-denied'});
});
test('admin lookup returns selected support fields without private account data',async()=>{
  const f=fixture();f.db.docs.set('communityProfiles/member',{username:'Cook',secret:'hidden'});
  f.db.docs.set('users/member',{recipes:['private']});
  const user=await f.call('owner',{action:'lookupUser',email:' member@example.com '});
  assert.equal(user.username,'Cook');assert.equal(user.uid,'member');assert.equal(user.recipes,undefined);assert.equal(user.secret,undefined);
  await assert.rejects(f.call('owner',{action:'lookupUser',email:'missing@example.com'}),{code:'not-found'});
});
test('post visibility and report changes commit with an audit entry and cannot restore unapproved content',async()=>{
  const f=fixture();
  await assert.rejects(f.call('owner',{action:'setPostHidden',postId:'post',hidden:true,reason:''}),{code:'invalid-argument'});
  await f.call('owner',{action:'setPostHidden',postId:'post',hidden:true,reason:'Abusive caption'});
  assert.equal(f.db.docs.get('feedPosts/post').hidden,true);
  let audit=await f.call('owner',{action:'audit'});assert.equal(audit.entries.length,1);assert.equal(audit.entries[0].actor,'owner');
  await f.call('owner',{action:'setPostHidden',postId:'post',hidden:false,reason:'Appeal reviewed'});
  f.db.docs.set('feedPosts/old',{authorId:'member',createdAt:1,hidden:true});
  await assert.rejects(f.call('owner',{action:'setPostHidden',postId:'old',hidden:false,reason:'Restore'}),{code:'failed-precondition'});
  f.db.docs.set('feedPosts/post/reports/member',{reason:'Offensive',createdAt:1,resolved:false});
  await f.call('owner',{action:'resolveReport',postId:'post',reporterId:'member',reason:'Reviewed'});
  assert.equal(f.db.docs.get('feedPosts/post/reports/member').resolved,true);
  audit=await f.call('owner',{action:'audit'});assert.equal(audit.entries.length,3);
});
test('admin Family grants are bounded, audited and independent of purchased entitlements',async()=>{
  const f=fixture();f.db.docs.set('billingEntitlements/member',{active:true,expiresAt:9999999999});
  for(const days of [-1,366,1.5,'30'])await assert.rejects(f.call('owner',{action:'setFamilyGrant',uid:'member',days,reason:'Test'}),{code:'invalid-argument'});
  await f.call('owner',{action:'setFamilyGrant',uid:'member',days:30,reason:'Family testing'});
  assert.equal(f.db.docs.get('familyGrants/member').expiresAt,1000000+30*86400000);
  await f.call('owner',{action:'setFamilyGrant',uid:'member',days:0,reason:'Testing ended'});
  assert.equal(f.db.docs.get('familyGrants/member').active,false);
  assert.equal(f.db.docs.get('billingEntitlements/member').active,true);
  assert.equal((await f.call('owner',{action:'audit'})).entries.length,2);
});
test('administrators can pause promotions but cannot fabricate payments or activate campaigns',async()=>{
  const f=fixture();f.db.docs.set('promotionCampaigns/post',{active:true,receiptId:'verified-receipt'});
  await f.call('owner',{action:'pausePromotion',postId:'post',reason:'Campaign review'});
  assert.equal(f.db.docs.get('promotionCampaigns/post').active,false);assert.equal(f.db.docs.get('promotionCampaigns/post').receiptId,'verified-receipt');
  await assert.rejects(f.call('owner',{action:'activatePromotion',postId:'post',paid:true}),{code:'invalid-argument'});
});
test('admin post pages include hidden posts and unresolved reports, without exposing private recipes',async()=>{
  const f=fixture();f.db.docs.get('feedPosts/post').hidden=true;
  f.db.docs.set('feedPosts/post/reports/member',{reason:'Please review',resolved:false,createdAt:1});
  const page=await f.call('owner',{action:'posts'});assert.equal(page.posts.length,1);assert.equal(page.posts[0].hidden,true);assert.equal(page.posts[0].reports[0].reason,'Please review');
});
