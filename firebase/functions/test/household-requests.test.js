const {test}=require('node:test');const assert=require('node:assert/strict');
const {randomUUID}=require('node:crypto');const {database}=require('./feed-database');
const {createHandler}=require('../application');const {createHouseholdMeals}=require('../household-meals');
function fixture(){
 const db=database();db.docs.set('households/home',{owner:'owner',name:'Home',members:{owner:'Owner',adult:'Adult',child:'Child',legacy:'Legacy'},roles:{adult:'adult',child:'restricted'}});
 for(const uid of ['owner','adult','child','legacy'])db.docs.set('householdAccounts/'+uid,{householdId:'home'});
 db.docs.set('familyGrants/owner',{active:true,expiresAt:Date.now()+86400000});
 const handler=createHandler(db),meals=createHouseholdMeals(db,{getUser:async()=>({})});
 const call=(uid,data)=>handler({auth:uid?{uid,token:{email:uid+'@example.com'}}:null,data:{householdId:'home',...data}});
 const meal=(uid,data)=>meals({auth:uid?{uid}:null,data:{householdId:'home',weekStart:'2026-10-19',...data}});
 const patch=(uid,changes)=>call(uid,{action:'patch',scope:'household',changes});
 const requestList=uid=>call(uid,{action:'requests'});
 const resolve=(uid,id,decision='approve')=>call(uid,{action:'resolveRequest',requestId:id,decision});
 return{db,call,meal,patch,requestList,resolve};
}
const item=(name='Milk')=>({Id:randomUUID(),Name:name,Quantity:'1 L',Category:'Dairy',Note:'',InBasket:false});
test('only owner sets roles; legacy members remain adults; owner cannot be demoted or spoofed',async()=>{
 const f=fixture();const status=await f.call('child',{action:'status'});assert.equal(status.role,'restricted');assert.equal(status.permissionsVersion,1);
 assert.equal((await f.call('legacy',{action:'status'})).role,'adult');
 for(const actor of ['adult','child'])await assert.rejects(f.call(actor,{action:'setRole',member:'legacy',role:'restricted'}),{code:'permission-denied'});
 for(const [member,role] of [['owner','restricted'],['outsider','adult'],['child','owner']])await assert.rejects(f.call('owner',{action:'setRole',member,role}),{code:'invalid-argument'});
 await f.call('owner',{action:'setRole',member:'legacy',role:'restricted'});assert.equal((await f.call('legacy',{action:'status'})).role,'restricted');
});
test('restricted additions become retry-safe requests and approval adds exactly once',async()=>{
 const f=fixture(),milk=item();const payload=[{before:null,after:milk}];
 for(let i=0;i<2;i++){const result=await f.patch('child',payload);assert.equal(result.items.length,0);assert.equal(result.requestedCount,1);}
 const list=await f.requestList('child');assert.equal(list.requests.length,1);assert.equal(list.canApprove,false);
 const id=list.requests[0].id;await assert.rejects(f.resolve('child',id),{code:'permission-denied'});
 await Promise.all([f.resolve('owner',id),f.resolve('adult',id)]);
 const actual=await f.call('adult',{action:'read',scope:'household'});assert.equal(actual.items.length,1);assert.equal(actual.items[0].Name,'Milk');
 assert.equal((await f.requestList('child')).requests[0].status,'approved');
});
test('restricted checks work, edits and deletes fail atomically, personal lists are unrestricted',async()=>{
 const f=fixture(),milk=item();await f.patch('owner',[{after:milk}]);
 await f.patch('child',[{before:milk,after:{...milk,InBasket:true}}]);
 const checked={...milk,InBasket:true};
 await assert.rejects(f.patch('child',[{before:checked,after:null}]),{code:'permission-denied'});
 await assert.rejects(f.patch('child',[{after:item('Treat')},{before:checked,after:{...checked,Name:'Spoof'}}]),{code:'permission-denied'});
 assert.equal((await f.requestList('owner')).requests.length,0);
 await f.call('child',{action:'patch',scope:'personal',changes:[{after:item('Personal')}]});
 assert.equal((await f.call('child',{action:'read',scope:'personal'})).items.length,1);
});
test('decline/cancel do not alter shopping and outsiders cannot read or resolve requests',async()=>{
 const f=fixture();await f.patch('child',[{after:item()}]);const id=(await f.requestList('owner')).requests[0].id;
 await assert.rejects(f.requestList('outsider'),{code:'failed-precondition'});
 await assert.rejects(f.call('adult',{action:'cancelRequest',requestId:id}),{code:'permission-denied'});
 await f.resolve('adult',id,'decline');await f.resolve('owner',id);
 assert.equal((await f.call('owner',{action:'read',scope:'household'})).items.length,0);
 await f.patch('child',[{after:item('Eggs')}]);const next=(await f.requestList('owner')).requests.find(r=>r.status==='pending');
 await f.call('child',{action:'cancelRequest',requestId:next.id});assert.equal((await f.requestList('owner')).requests.find(r=>r.id===next.id).status,'cancelled');
});
test('new meals from restricted members wait for adult approval; edits/deletes stay forbidden',async()=>{
 const f=fixture(),meal={id:'a'.repeat(32),date:'2026-10-20',slot:'dinner',title:'Lasagne',servings:4,note:'Please',recipe:null};
 const send=()=>f.meal('child',{action:'upsertMeal',meal,expectedVersion:0});assert.equal((await send()).requested,true);await send();
 assert.equal((await f.meal('owner',{action:'readWeek'})).meals.length,0);
 const entry=(await f.requestList('owner')).requests[0];await f.resolve('adult',entry.id);
 const week=await f.meal('child',{action:'readWeek'});assert.equal(week.meals[0].title,'Lasagne');assert.equal(week.meals[0].updatedBy,'adult');
 await assert.rejects(f.meal('child',{action:'upsertMeal',meal:{...meal,title:'Changed'},expectedVersion:1}),{code:'permission-denied'});
 await assert.rejects(f.meal('child',{action:'deleteMeal',mealId:meal.id,expectedVersion:1}),{code:'permission-denied'});
});
test('adults can suggest meals, quota and role/membership/entitlement changes are enforced',async()=>{
 const f=fixture(),meal={id:'b'.repeat(32),date:'2026-10-21',slot:'dinner',title:'Soup',servings:2,note:'',recipe:null};
 await f.meal('adult',{action:'requestMeal',meal,expectedVersion:0});const id=(await f.requestList('owner')).requests[0].id;
 await f.call('owner',{action:'setRole',member:'adult',role:'restricted'});await assert.rejects(f.resolve('adult',id),{code:'permission-denied'});
 delete f.db.docs.get('households/home').members.adult;await assert.rejects(f.resolve('owner',id),{code:'failed-precondition'});
 const home=f.db.docs.get('households/home');home.requestRates={child:{day:new Date().toISOString().slice(0,10),count:100}};
 await assert.rejects(f.patch('child',[{after:item()}]),{code:'resource-exhausted'});
 f.db.docs.delete('familyGrants/owner');await assert.rejects(f.requestList('owner'),{code:'permission-denied'});
});
test('invitation role is chosen by the owner and cannot be upgraded by the joining member',async()=>{
 const f=fixture();delete f.db.docs.get('households/home').members.legacy;f.db.docs.delete('householdAccounts/legacy');
 const invite=await f.call('owner',{action:'invite',role:'restricted'});
 await f.call('newchild',{action:'join',code:invite.code,role:'adult'});
 assert.equal((await f.call('newchild',{action:'status'})).role,'restricted');
});

test('batch quotas fail atomically and leaving clears role/request metadata',async()=>{
 const f=fixture();await assert.rejects(f.patch('child',Array.from({length:51},()=>({after:item()}))),{code:'resource-exhausted'});
 assert.equal((await f.requestList('owner')).requests.length,0);
 await f.patch('child',[{after:item()}]);await f.call('child',{action:'leave'});
 const home=f.db.docs.get('households/home');assert(!home.roles.child);assert(!home.requestRates.child);assert.equal(Object.values(home.requests).length,0);
});
test('approval conflicts preserve the pending request without overwriting an edited item',async()=>{
 const f=fixture(),milk=item();await f.patch('child',[{after:milk}]);const id=(await f.requestList('owner')).requests[0].id;
 await f.patch('adult',[{after:{...milk,Quantity:'3 L'}}]);
 await assert.rejects(f.resolve('owner',id),{code:'aborted'});
 assert.equal((await f.requestList('owner')).requests[0].status,'pending');
 assert.equal((await f.call('owner',{action:'read',scope:'household'})).items[0].Quantity,'3 L');
});

test('household rename is owner-only, trimmed and preserves membership and shopping',async()=>{
 const f=fixture();await f.patch('owner',[{after:item()}]);
 assert.equal((await f.call('owner',{action:'status'})).canRenameHousehold,true);
 for(const uid of ['adult','child'])await assert.rejects(f.call(uid,{action:'rename',name:'Spoof'}),{code:'permission-denied'});
 await assert.rejects(f.call('outsider',{action:'rename',name:'Spoof'}),{code:'failed-precondition'});
 await assert.rejects(f.call('owner',{action:'rename',householdId:'other',name:'Spoof'}),{code:'permission-denied'});
 const shopping=structuredClone(f.db.docs.get('households/home/lists/shopping'));
 const before=structuredClone(f.db.docs.get('households/home'));f.db.docs.delete('familyGrants/owner');
 await f.call('owner',{action:'rename',name:'  Family groceries  '});
 assert.deepEqual(f.db.docs.get('households/home'),{...before,name:'Family groceries'});
 assert.equal((await f.call('child',{action:'status'})).name,'Family groceries');
 assert.deepEqual(f.db.docs.get('households/home/lists/shopping'),shopping);
});
test('household rename rejects invalid names without changing the name',async()=>{
 const f=fixture();for(const name of [null,12,'','   ','a'.repeat(81)])await assert.rejects(f.call('owner',{action:'rename',name}),{code:'invalid-argument'});
 assert.equal(f.db.docs.get('households/home').name,'Home');
});

test('shared additions get trusted attribution, survive old-client edits/deletes, and cannot be spoofed',async()=>{
 const f=fixture(),milk=item();
 await f.patch('adult',[{after:{...milk,AddedBy:'owner',AddedByName:'Forged',AdditionKind:'requested'}}]);
 let stored=(await f.call('owner',{action:'read',scope:'household'})).items[0];
 assert.equal(stored.AddedBy,'adult');assert.equal(stored.AddedByName,'Adult');assert.equal(stored.AdditionKind,'added');
 await f.patch('owner',[{after:milk}]); // Lost response retry must not change the author.
 await f.patch('owner',[{before:milk,after:{...milk,Quantity:'2 L',AddedBy:'owner'}}]);
 stored=(await f.call('owner',{action:'read',scope:'household'})).items[0];assert.equal(stored.AddedBy,'adult');assert.equal(stored.Quantity,'2 L');
 await f.patch('child',[{before:{...milk,Quantity:'2 L'},after:{...milk,Quantity:'2 L',InBasket:true}}]);
 stored=(await f.call('owner',{action:'read',scope:'household'})).items[0];assert.equal(stored.AddedBy,'adult');
 await f.patch('owner',[{before:{...milk,Quantity:'2 L',InBasket:true},after:null}]);assert.equal((await f.call('owner',{action:'read',scope:'household'})).items.length,0);
 await f.call('adult',{action:'patch',scope:'personal',changes:[{after:milk}]});
 assert.equal((await f.call('adult',{action:'read',scope:'personal'})).items[0].AddedBy,undefined);
});
test('shopping attribution uses account names for new and existing items without disclosing email',async()=>{
 const f=fixture(),milk=item();
 f.db.docs.get('households/home').members.adult='alex@example.com';
 f.db.docs.set('users/adult',{first_name:' Alex ',last_name:' Smith '});
 await f.patch('adult',[{after:{...milk,AddedByName:'Spoof'}}]);
 assert.equal(f.db.docs.get('households/home/lists/shopping').items[milk.Id].AddedByName,'Alex Smith');
 f.db.docs.get('households/home/lists/shopping').items[milk.Id].AddedByName='alex@example.com';
 f.db.docs.set('users/adult',{first_name:'Alexandra',last_name:'Smith'});
 assert.equal((await f.call('owner',{action:'read',scope:'household'})).items[0].AddedByName,'Alexandra Smith');
 f.db.docs.delete('users/adult');
 assert.equal((await f.call('owner',{action:'read',scope:'household'})).items[0].AddedByName,'Household member');
});

test('approved shopping keeps requester attribution across approvals and retries',async()=>{
 const f=fixture();await f.patch('child',[{after:item()}]);const request=(await f.requestList('owner')).requests[0];
 await f.resolve('adult',request.id);await f.resolve('owner',request.id);
 const stored=(await f.call('owner',{action:'read',scope:'household'})).items[0];
 assert.equal(stored.AddedBy,'child');assert.equal(stored.AddedByName,'Child');assert.equal(stored.AdditionKind,'requested');
});

test('leaving and removal anonymize contributions without removing shopping items',async()=>{
 for(const action of ['leave','remove']){
  const f=fixture();await f.patch('adult',[{after:item()}]);
  await f.call(action==='leave'?'adult':'owner',{action,member:'adult'});
  const stored=(await f.call('owner',{action:'read',scope:'household'})).items[0];
  assert.equal(stored.Name,'Milk');assert.equal(stored.AddedBy,'deleted-user');assert.equal(stored.AddedByName,'Former member');
 }
});
