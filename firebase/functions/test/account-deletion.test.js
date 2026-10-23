const {test}=require('node:test');const assert=require('node:assert/strict');
const {createAccountDeletion}=require('../account-deletion');
function fixture(){
 let now=1800000000000,failOnce=false;const docs=new Map(),users=new Map([['alice',{}],['bob',{}]]);
 const copy=x=>x===undefined?undefined:structuredClone(x);
 function ref(path){return{path,id:path.split('/').at(-1),get:async()=>snapshot(path),collection:name=>query(path+'/'+name),set:async v=>docs.set(path,copy(v)),update:async v=>{assert(docs.has(path));docs.set(path,{...docs.get(path),...copy(v)});}};}
 function snapshot(path){return{id:path.split('/').at(-1),ref:ref(path),exists:docs.has(path),data:()=>copy(docs.get(path))};}
 function query(path,group=false,filters=[],after='',max=Infinity){return{
  doc:id=>ref(path+'/'+id),where:(field,op,value)=>{assert.equal(op,'==');return query(path,group,[...filters,[field,value]],after,max);},
  orderBy:()=>query(path,group,filters,after,max),startAfter:id=>query(path,group,filters,id,max),limit:n=>query(path,group,filters,after,n),
  get:async()=>{const result=[...docs.keys()].filter(p=>group?p.split('/').at(-2)===path:p.startsWith(path+'/')&&p.split('/').length===path.split('/').length+1).map(snapshot).filter(d=>d.id>after&&filters.every(([k,v])=>d.data()[k]===v)).sort((a,b)=>a.id.localeCompare(b.id)).slice(0,max);return{docs:result,size:result.length};}
 };}
 let queue=Promise.resolve();
 const db={doc:ref,collection:path=>query(path),collectionGroup:path=>query(path,true),recursiveDelete:async r=>{if(failOnce){failOnce=false;throw Error('Injected retry');}for(const p of [...docs.keys()])if(p===r.path||p.startsWith(r.path+'/'))docs.delete(p);},
  runTransaction:action=>{const call=queue.then(async()=>{const writes=[];const tx={get:async r=>{assert.equal(writes.length,0);return r.get();},create:(r,v)=>{assert(!docs.has(r.path));writes.push(()=>docs.set(r.path,copy(v)));},set:(r,v)=>writes.push(()=>docs.set(r.path,copy(v))),update:(r,v)=>writes.push(()=>{assert(docs.has(r.path));docs.set(r.path,{...docs.get(r.path),...copy(v)});}),delete:r=>writes.push(()=>docs.delete(r.path))};const result=await action(tx);writes.forEach(w=>w());return result;});queue=call.catch(()=>{});return call;}
 };
 const missing=()=>Object.assign(Error('Missing'),{code:'auth/user-not-found'});
 const auth={getUser:async uid=>{if(!users.has(uid))throw missing();return users.get(uid);},updateUser:async(uid,v)=>{if(!users.has(uid))throw missing();users.set(uid,{...users.get(uid),...v});},revokeRefreshTokens:async()=>{},deleteUser:async uid=>{if(!users.delete(uid))throw missing();}};
 const service=createAccountDeletion(db,auth,()=>now),request=(data={},uid='alice',age=0)=>service.handle({auth:uid?{uid,token:{auth_time:(now-age)/1000}}:null,data:{action:'request',confirmation:'DELETE',deleteOwnedSpaces:true,...data}});
 return{docs,users,service,request,advance:()=>now+=120001,fail:()=>failOnce=true};
}
test('deletion requires recent authentication, literal confirmation and explicit owned-space consent',async()=>{
 const f=fixture();for(const args of [[{},null],[{},'alice',300001],[{confirmation:'delete'}],[{deleteOwnedSpaces:false}],[{uid:'bob'}]])await assert.rejects(f.request(...args));assert.equal(f.docs.size,0);
});
test('request only creates a durable job and is idempotent even once the account is disabled',async()=>{
 const f=fixture();f.docs.set('users/alice',{email:'private'});const a=await f.request();assert(a.accepted);assert(f.docs.has('users/alice'));f.users.set('alice',{disabled:true});const b=await f.request();assert.equal(a.reference,b.reference);assert.equal(f.docs.size,2);
});
test('owner cleanup removes shared spaces and matching member indexes while preserving other private accounts',async()=>{
 const f=fixture();f.docs.set('households/home',{owner:'alice',members:{alice:'A',bob:'B'}});f.docs.set('households/home/weeks/2026',{meals:'private'});f.docs.set('householdAccounts/alice',{householdId:'home'});f.docs.set('householdAccounts/bob',{householdId:'home'});f.docs.set('users/alice/recipes/r',{photo:'private'});f.docs.set('users/bob/recipes/keep',{name:'mine'});f.docs.set('users/alice',{});
 await f.request();await f.service.process('alice');assert.equal(f.docs.get('accountDeletions/alice').status,'settling');assert(f.users.get('alice').disabled);assert(f.users.has('alice'));assert(!f.docs.has('householdAccounts/bob'));assert(!f.docs.has('households/home/weeks/2026'));assert(f.docs.has('users/bob/recipes/keep'));
 // A late in-flight write is removed by the settling sweep before Auth deletion.
 f.docs.set('users/alice/recipes/late',{name:'late'});f.advance();await f.service.process('alice');assert(!f.docs.has('users/alice/recipes/late'));assert(!f.users.has('alice'));assert(f.users.has('bob'));assert.equal(f.docs.get('accountDeletions/alice').status,'completed');await f.service.process('alice');
});
test('a member leaves without deleting the household or another member\'s data',async()=>{
 const f=fixture();f.docs.set('households/home',{owner:'bob',members:{alice:'A',bob:'B'},roles:{alice:'restricted'},requestRates:{alice:{count:1}},requests:{own:{authorId:'alice'},other:{authorId:'bob',resolvedBy:'alice'}}});f.docs.set('householdAccounts/alice',{householdId:'home'});f.docs.set('households/home/weeks/week',{meals:'keep'});await f.request();await f.service.process('alice');assert.deepEqual(f.docs.get('households/home').members,{bob:'B'});assert(f.docs.has('households/home/weeks/week'));assert(!f.docs.get('households/home').roles.alice);assert(!f.docs.get('households/home').requests.own);assert.equal(f.docs.get('households/home').requests.other.resolvedBy,'deleted-user');
});
test('partial cleanup retries without reporting completion or losing the job',async()=>{
 const f=fixture();f.docs.set('users/alice',{});await f.request();f.fail();await assert.rejects(f.service.process('alice'));assert.equal(f.docs.get('accountDeletions/alice').status,'retry');assert(f.users.has('alice'));f.advance();await f.service.process('alice');assert(!f.users.has('alice'));assert.equal(f.docs.get('accountDeletions/alice').status,'completed');
});
test('legacy contributions and purchase links are removed while unrelated contributions remain',async()=>{
 const f=fixture();f.docs.set('feedPosts/own',{authorId:'alice'});f.docs.set('feedPosts/own/comments/c',{authorId:'bob'});f.docs.set('feedPosts/other',{authorId:'bob',likeCount:1});f.docs.set('feedPosts/other/likes/alice',{createdAt:1});f.docs.set('feedPosts/other/comments/remove',{authorId:'alice',text:'remove'});f.docs.set('feedPosts/other/comments/keep',{authorId:'bob',text:'keep'});f.docs.set('billingPurchases/p',{uid:'alice',token:'secret'});f.docs.set('feedAccounts/bob',{following:{alice:'A',carol:'C'}});await f.request();await f.service.process('alice');assert(!f.docs.has('feedPosts/own/comments/c'));assert(!f.docs.has('feedPosts/other/comments/remove'));assert(f.docs.has('feedPosts/other/comments/keep'));assert(!f.docs.has('billingPurchases/p'));assert.deepEqual(f.docs.get('feedAccounts/bob').following,{carol:'C'});assert.equal(f.docs.get('feedPosts/other').likeCount,0);
});
test('preview reports owned spaces without mutation and active worker leases prevent duplicate work',async()=>{
 const f=fixture();f.docs.set('households/home',{owner:'alice',name:'Family'});const preview=await f.request({action:'prepare'});assert.equal(preview.ownedHouseholds[0].name,'Family');assert(!f.docs.has('accountDeletions/alice'));await f.request();await f.service.process('alice');const before=structuredClone(f.docs.get('accountDeletions/alice'));await f.service.process('alice');assert.deepEqual(f.docs.get('accountDeletions/alice'),before);
});
test('owned creator spaces are deleted without erasing collaborators personal accounts or other workspaces',async()=>{
 const f=fixture();f.docs.set('businesses/owned',{owner:'alice',members:{alice:'owner',bob:'editor'}});f.docs.set('businesses/other',{owner:'bob',members:{alice:'editor',bob:'owner'}});f.docs.set('businessAccounts/alice',{ids:['owned','other']});f.docs.set('businessAccounts/bob',{ids:['owned','other']});f.docs.set('businessCollections/c',{businessId:'owned'});f.docs.set('users/bob',{email:'keep'});await f.request();await f.service.process('alice');assert(!f.docs.has('businesses/owned'));assert(!f.docs.has('businessCollections/c'));assert.deepEqual(f.docs.get('businesses/other').members,{bob:'owner'});assert.deepEqual(f.docs.get('businessAccounts/bob').ids,['other']);assert(f.docs.has('users/bob'));
});
