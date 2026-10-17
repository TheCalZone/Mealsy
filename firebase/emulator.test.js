// Run only through firebase emulators:exec, using a demo project (never production).
const {test, before, after} = require('node:test');
const assert = require('node:assert/strict');
const {readFileSync} = require('node:fs');
const {createRequire} = require('node:module');
const localRequire = createRequire(`${__dirname}/functions/package.json`);
const {initializeTestEnvironment, assertFails, assertSucceeds} = localRequire('@firebase/rules-unit-testing');
const {doc, getDoc, setDoc} = localRequire('firebase/firestore');
const {initializeApp, deleteApp} = localRequire('firebase-admin/app');
const {getFirestore} = localRequire('firebase-admin/firestore');
const {createHandler} = require('./functions/application');
let rules, app, db, handle;
before(async () => {
  assert(process.env.FIRESTORE_EMULATOR_HOST, 'Start Firestore emulator first; production is prohibited');
  rules = await initializeTestEnvironment({projectId: 'demo-mealsy', firestore: {rules: readFileSync(`${__dirname}/firestore.rules`, 'utf8')}});
  await rules.clearFirestore();
  app = initializeApp({projectId: 'demo-mealsy'}, 'household-tests'); db = getFirestore(app); handle = createHandler(db);
  await db.doc('billingEntitlements/owner').set({active: true, expiresAt: Date.now() + 86400000});
});
after(async () => { if (rules) await rules.cleanup(); if (app) await deleteApp(app); });
const call = (uid, data) => handle({auth: uid ? {uid, token: {email: `${uid}@example.com`}} : null, data});
test('business collection writes and impression debits use real atomic transactions',async()=>{
 const now=Date.now();await db.doc('businesses/emulator-business').set({owner:'business-owner',name:'Test kitchen',verified:true,members:{'business-owner':'owner'},createdAt:now});
 await db.doc('feedPosts/emulator-business-recipe').set({businessId:'emulator-business',authorId:'business-owner',recipe:{title:'Soup'},createdAt:now,moderation:{status:'approved'},hidden:false});
 const business=require('./functions/business').createBusiness(db,{getUser:async()=>({emailVerified:true})},()=>now,async()=>({status:'approved'}));
 await business({auth:{uid:'business-owner'},data:{action:'saveCollection',businessId:'emulator-business',id:'emulator-collection',title:'Week',postIds:['emulator-business-recipe'],meals:[{day:0,slot:2,postId:'emulator-business-recipe'}],published:true}});
 const collection=await business({auth:{uid:'reader'},data:{action:'collection',collectionId:'emulator-collection'}});assert.equal(collection.posts.length,1);
 await db.doc('commercialConfig/campaigns').set({enabled:true});await db.doc('businessWallets/emulator-business').set({balanceMicro:1000});
 await db.doc('businessCampaigns/emulator-campaign').set({businessId:'emulator-business',postId:'emulator-business-recipe',startsAt:now,endsAt:0,status:'paused',budgetMinor:100,dailyBudgetMinor:100});
 let clock=now;const ledger=require('./functions/campaign-ledger').createCampaignLedger(db,()=>clock);const request=(uid,data)=>ledger({auth:{uid},app:{appId:'emulator-verified'},data});
 await request('business-owner',{action:'start',campaignId:'emulator-campaign'});
 const a=await request('viewer-a',{action:'prepareImpression',postId:'emulator-business-recipe'}),b=await request('viewer-b',{action:'prepareImpression',postId:'emulator-business-recipe'});clock+=1100;
 const result=await Promise.all([request('viewer-a',{action:'impression',postId:'emulator-business-recipe',ticket:a.ticket}),request('viewer-b',{action:'impression',postId:'emulator-business-recipe',ticket:b.ticket})]);assert.equal(result.filter(r=>r.charged).length,1);assert.equal((await db.doc('businessWallets/emulator-business').get()).data().balanceMicro,0);
});
test('rules preserve private recipes and deny direct access to household/server documents', async () => {
  const parent = rules.authenticatedContext('parent').firestore();
  const child = rules.authenticatedContext('child').firestore();
  const guest = rules.unauthenticatedContext().firestore();
  await assertSucceeds(setDoc(doc(parent, 'users/parent/recipes/1'), {Name: 'Private recipe'}));
  await assertFails(getDoc(doc(child, 'users/parent/recipes/1')));
  await assertFails(getDoc(doc(guest, 'users/parent/recipes/1')));
  for (const path of ['businesses/fake','businessAccounts/parent','businessInvites/fake','businessCollections/fake','businessCampaigns/fake','businessWallets/fake','businessMetrics/fake','businessEvents/fake','commercialConfig/campaigns','promotionPurchases/fake','impressionTickets/fake','billableImpressions/fake','seatEntitlements/parent','notificationDevices/fake','notifications/parent/items/fake','notifications/child/items/fake','adminAudit/fake','communityProfiles/parent','communityNames/private','promotionReceipts/fake','promotionCampaigns/fake','promotionRequests/fake','familyGrants/parent', 'feedAccounts/parent', 'feedAccounts/child', 'households/fake', 'householdAccounts/parent', 'householdInvites/fake', 'shoppingLists/parent', 'households/fake/lists/shopping']) {
    await assertFails(setDoc(doc(parent, path), {owner: 'parent'}));
    await assertFails(getDoc(doc(parent, path)));
  }
});

test('real social transactions persist interests, following and learned likes privately', async () => {
  const feed = require('./functions/feed').createFeedHandler(db, Date.now, async()=>({status:'approved',provider:'emulator-test'}));
  const call = (uid, data) => feed({auth: {uid, token: {email_verified:true}}, data});
  await call('social-cook',{action:'setUsername',username:'Cook'});
  await call('social-cook', {action: 'publish', id: 'social-pasta', caption: 'Quick Italian pasta', authorName: 'Cook', photo: '', recipe: null});
  await call('social-reader', {action: 'savePreferences', interests: ['Italian']});
  await call('social-reader', {action: 'follow', postId: 'social-pasta', following: true});
  await Promise.all([true, true].map(liked => call('social-reader', {action: 'like', postId: 'social-pasta', liked})));
  const page = await call('social-reader', {action: 'list', mode: 'following', search: 'pasta'});
  assert.equal(page.posts.length, 1); assert(page.posts[0].following); assert(page.posts[0].liked);
  assert.equal((await db.doc('feedAccounts/social-reader').get()).data().affinities.Italian, 1);
  assert.deepEqual((await call('social-other', {action: 'preferences'})).following, []);
  await call('social-cook', {action: 'delete', postId: 'social-pasta'});
});

test('administrator transactions enforce fresh roles, audit changes and manage Family access', async () => {
  let enabled = true;
  const auth = {getUser:async uid => ({uid,customClaims:{admin:uid==='admin-user' && enabled}})};
  const admin = require('./functions/administration').createAdministration(db,auth);
  const request = data => admin({auth:{uid:'admin-user',token:{}},data});
  await request({action:'setFamilyGrant',uid:'admin-family-owner',days:7,reason:'Household support'});
  const {householdId} = await call('admin-family-owner',{action:'create',name:'Supported household'});
  assert.deepEqual((await call('admin-family-owner',{action:'read',scope:'household',householdId})).items,[]);
  await request({action:'setFamilyGrant',uid:'admin-family-owner',days:0,reason:'Support ended'});
  await assert.rejects(call('admin-family-owner',{action:'read',scope:'household',householdId}),{code:'permission-denied'});
  await db.doc('feedPosts/admin-review').set({createdAt:Date.now(),authorId:'cook',caption:'Example',moderation:{status:'approved'},hidden:false});
  await request({action:'setPostHidden',postId:'admin-review',hidden:true,reason:'Review content'});
  assert.equal((await db.doc('feedPosts/admin-review').get()).data().hidden,true);
  const audit = await request({action:'audit'});assert.equal(audit.entries.length,3);
  const client = rules.authenticatedContext('admin-user',{admin:true}).firestore();
  await assertFails(setDoc(doc(client,'familyGrants/fake'),{active:true}));
  await assertFails(getDoc(doc(client,`adminAudit/${audit.entries[0].id}`)));
  enabled=false;
  await assert.rejects(request({action:'audit'}),{code:'permission-denied'});
});
test('real transactions enforce single-use acceptance, shared editing, privacy and revocation', async () => {
  const {householdId} = await call('owner', {action: 'create', name: 'Test family'});
  const {code} = await call('owner', {action: 'invite'});
  const joins = await Promise.allSettled(['alice', 'bob'].map(uid => call(uid, {action: 'join', code})));
  assert.equal(joins.filter(r => r.status === 'fulfilled').length, 1);
  const member = joins[0].status === 'fulfilled' ? 'alice' : 'bob';
  const item = {Id: '11111111-1111-1111-1111-111111111111', Name: 'Milk', Category: 'Dairy', Quantity: '', Note: '', InBasket: false};
  await call(member, {action: 'patch', scope: 'household', householdId, changes: [{after: item}]});
  assert.equal((await call('owner', {action: 'read', scope: 'household', householdId})).items.length, 1);
  // Match System.Net.Http.Json's default web serialization used by the APK.
  const bread = {id: '22222222-2222-2222-2222-222222222222', name: 'Bread', category: 'Bakery', quantity: '', note: '', inBasket: false};
  const patch = {action: 'patch', scope: 'household', householdId, changes: [{before: null, after: bread}]};
  await call(member, patch); await call(member, patch);
  const shared = (await call('owner', {action: 'read', scope: 'household', householdId})).items;
  assert.equal(shared.length, 2); assert.equal(shared.find(i => i.Id === bread.id).Name, 'Bread');
  await call('owner', {...patch, changes: [{before: bread, after: {...bread, inBasket: true}}]});
  assert.equal((await call(member, {action: 'read', scope: 'household', householdId})).items.find(i => i.Id === bread.id).InBasket, true);
  await call('owner', {action: 'patch', scope: 'personal', changes: [{after: item}]});
  assert.deepEqual((await call(member, {action: 'read', scope: 'personal', uid: 'owner'})).items, []);
  await call('owner', {action: 'remove', member});
  await assert.rejects(call(member, {action: 'read', scope: 'household', householdId}), {code: 'failed-precondition'});
});
test('feed Firestore transactions count likes once and enforce hidden-post access', async () => {
  const feed = require('./functions/feed').createFeedHandler(db, Date.now, async()=>({status:'approved',provider:'emulator-test'}));
  const request = (uid, data, moderator = false) => feed({auth: {uid, token: {email_verified:true,feedModerator: moderator}}, data});
  await request('feed-owner',{action:'setUsername',username:'Cook'});
  await request('reader',{action:'setUsername',username:'Reader'});
  await request('feed-owner', {action: 'publish', id: 'real-feed-post', caption: 'A real emulator meal', authorName: 'Cook', recipe: null, photo: ''});
  await Promise.all(Array.from({length: 4}, () => request('reader', {action: 'like', postId: 'real-feed-post', liked: true})));
  const listed = await request('reader', {action: 'list'});
  const target=listed.posts.find(p=>p.id==='real-feed-post');assert.equal(target.likeCount, 1); assert.equal(target.liked, true);
  await request('reader', {action: 'comment', postId: 'real-feed-post', id: 'real-comment', text: 'Looks delicious', authorName: 'Reader'});
  assert.equal((await request('reader', {action: 'comments', postId: 'real-feed-post'})).comments.length, 1);
  await request('mod', {action: 'moderate', postId: 'real-feed-post', hidden: true}, true);
  assert(!(await request('reader', {action: 'list'})).posts.some(p=>p.id==='real-feed-post'));
  await assert.rejects(request('reader', {action: 'comments', postId: 'real-feed-post'}), {code: 'not-found'});
  const signedIn = rules.authenticatedContext('feed-owner').firestore();
  for (const path of ['feedPosts/real-feed-post', 'feedPosts/real-feed-post/likes/reader', 'feedPosts/real-feed-post/comments/real-comment', 'feedPosts/real-feed-post/reports/reader', 'feedLimits/feed-owner']) {
    await assertFails(getDoc(doc(signedIn, path))); await assertFails(setDoc(doc(signedIn, path), {authorId: 'feed-owner'}));
  }
});
