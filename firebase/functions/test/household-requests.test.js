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
