const {test}=require('node:test');const assert=require('node:assert/strict');
const {database}=require('./feed-database');const {createShoppingQuestions}=require('../shopping-questions');const {createHandler}=require('../application');const {createNotifications}=require('../notifications');
const itemId='11111111-1111-1111-1111-111111111111';
function fixture(){const db=database();db.docs.set('households/home',{owner:'owner',members:{owner:'Owner',adult:'Alex',child:'Sam'},roles:{child:'restricted'}});for(const uid of ['owner','adult','child'])db.docs.set('householdAccounts/'+uid,{householdId:'home'});db.docs.set('familyGrants/owner',{active:true,expiresAt:Date.now()+86400000});db.docs.set('households/home/lists/shopping',{items:{[itemId]:{Id:itemId,Name:'Mackerel',Category:'Meat',Quantity:'1',Note:'',InBasket:false,AddedBy:'adult',AddedByName:'Alex',AdditionKind:'added'}}});const handler=createShoppingQuestions(db);const call=(uid,data)=>handler({auth:uid?{uid}:null,data:{householdId:'home',itemId,...data}});return{db,call,send:(uid,text='Tinned or fresh?',id='a'.repeat(32),extra={})=>call(uid,{action:'send',text,messageId:id,...extra})};}
test('question alerts the trusted adder, reply alerts asker, retries do not duplicate',async()=>{
 const f=fixture();await f.send('child');await f.send('child');let inbox=[...f.db.docs.entries()].filter(([k])=>k.startsWith('notifications/'));assert.equal(inbox.length,1);assert(inbox[0][0].startsWith('notifications/adult/'));assert.equal(inbox[0][1].type,'householdQuestion');assert(!inbox[0][1].message.includes('Tinned'));
 await f.send('adult','Fresh please','b'.repeat(32));const thread=await f.call('owner',{action:'read'});assert.equal(thread.messages.length,2);assert.equal(thread.messages[0].authorName,'Sam');assert.equal(thread.targetId,'adult');assert([...f.db.docs.keys()].some(k=>k.startsWith('notifications/child/')));
 await assert.rejects(f.send('child','Changed question'),{code:'invalid-argument'});
});
test('access is checked for every action and personal/unavailable items cannot be queried',async()=>{
 const f=fixture();for(const action of ['read','send','clear'])await assert.rejects(f.call('outsider',{action}),{code:'permission-denied'});await assert.rejects(f.call(null,{action:'read'}),{code:'unauthenticated'});
 await assert.rejects(f.call('child',{action:'clear'}),{code:'permission-denied'});
 await assert.rejects(f.call('owner',{action:'read',itemId:'not-an-id'}),{code:'invalid-argument'});
 await assert.rejects(f.call('owner',{action:'read',itemId:'22222222-2222-2222-2222-222222222222'}),{code:'not-found'});
 f.db.docs.delete('familyGrants/owner');await assert.rejects(f.call('child',{action:'read'}),{code:'permission-denied'});
});
test('legacy items require a valid member recipient; sender cannot notify an outsider',async()=>{
 const f=fixture();delete f.db.docs.get('households/home/lists/shopping').items[itemId].AddedBy;
 await assert.rejects(f.send('child'),{code:'invalid-argument'});await assert.rejects(f.send('child','Question','a'.repeat(32),{recipientId:'outsider'}),{code:'invalid-argument'});
 await f.send('child','Question','a'.repeat(32),{recipientId:'owner'});assert([...f.db.docs.keys()].some(k=>k.startsWith('notifications/owner/')));
});
test('message length and daily quota bound writes; history is limited to 40 and adults can clear',async()=>{
 const f=fixture();await assert.rejects(f.send('child','x'.repeat(1001)),{code:'invalid-argument'});
 for(let n=0;n<60;n++)await f.send('child','Question '+n,n.toString(16).padStart(32,'0'));
 await assert.rejects(f.send('child','One more','f'.repeat(32)),{code:'resource-exhausted'});
 assert.equal((await f.call('adult',{action:'read'})).messages.length,40);
 await f.call('adult',{action:'clear'});assert.equal((await f.call('adult',{action:'read'})).messages.length,0);
});
test('conversation remains readable after item removal and leaving anonymizes messages and participants',async()=>{
 const f=fixture();await f.send('child');f.db.docs.get('households/home/lists/shopping').items={};assert.equal((await f.call('adult',{action:'read'})).title,'Mackerel');
 await createHandler(f.db)({auth:{uid:'child',token:{}},data:{action:'leave',householdId:'home'}});
 const value=f.db.docs.get('households/home/conversations/shopping');assert(!JSON.stringify(value).includes('child'));assert.equal((await f.call('adult',{action:'read'})).messages[0].authorName,'Former member');
});
test('paused social inbox keeps household alerts and excludes former households',async()=>{
 const f=fixture();await f.send('child');f.db.docs.set('notifications/adult/items/'+ 'd'.repeat(64),{type:'comment',message:'Social',createdAt:Date.now()});
 const notifications=createNotifications(f.db,{socialEnabled:false});const read=()=>notifications({auth:{uid:'adult'},data:{action:'list'}});
 assert.equal((await read()).items.length,1);assert.equal((await read()).items[0].itemId,itemId);
 f.db.docs.get('households/home').members={owner:'Owner'};assert.equal((await read()).items.length,0);
});
test('conversation browsing is household-only and storage limits reject without notifications',async()=>{
 const f=fixture();await f.send('child');assert.equal((await f.call('owner',{action:'list',itemId:undefined})).threads[0].title,'Mackerel');
 await assert.rejects(f.call('outsider',{action:'list',itemId:undefined}),{code:'permission-denied'});
 const value=f.db.docs.get('households/home/conversations/shopping');value.threads[itemId].messages[0].text='x'.repeat(500000);
 const before=structuredClone([...f.db.docs.entries()]);await assert.rejects(f.send('adult','Fresh please','c'.repeat(32)),{code:'resource-exhausted'});assert.deepEqual([...f.db.docs.entries()],before);
});
