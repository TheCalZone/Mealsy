const {fail,requireMember}=require('./domain');
const {requireFamily}=require('./entitlements');
const {canManage}=require('./household-requests');
const {requestData}=require('./request');
const {notify}=require('./notifications');
const validItem=id=>typeof id==='string'&&/^[a-f0-9]{8}(-[a-f0-9]{4}){3}-[a-f0-9]{12}$/.test(id);
function forgetQuestions(value,uid){
 if(!value)return null;
 const threads=Object.fromEntries(Object.entries(value.threads||{}).map(([id,t])=>[id,{...t,targetId:t.targetId===uid?'former-member':t.targetId,participants:(t.participants||[]).filter(p=>p!==uid),messages:t.messages.map(m=>m.authorId===uid?{...m,authorId:'former-member'}:m)}]));
 const rates={...value.rates};delete rates[uid];return{...value,threads,rates};
}
function createShoppingQuestions(db){return async request=>{
 if(!request.auth)fail('unauthenticated','Sign in to ask your household.');
 const uid=request.auth.uid,data=requestData(request,['list','read','send','clear']);
 if(typeof data.householdId!=='string'||!data.householdId||data.householdId.length>150||data.householdId.includes('/')||(data.action!=='list'&&!validItem(data.itemId)))fail('invalid-argument','Choose a household shopping item.');
 return db.runTransaction(async tx=>{
  const homeRef=db.doc(`households/${data.householdId}`),home=(await tx.get(homeRef)).data();requireMember(home,uid);await requireFamily(tx,db,home.owner);
  const ref=homeRef.collection('conversations').doc('shopping'),value=(await tx.get(ref)).data()||{threads:{},rates:{}};
  if(data.action==='list')return{threads:Object.entries(value.threads||{}).map(([itemId,t])=>({itemId,title:t.title,count:t.messages.length,updatedAt:t.messages.at(-1)?.createdAt||0})).sort((a,b)=>b.updatedAt-a.updatedAt)};
  const item=(await tx.get(homeRef.collection('lists').doc('shopping'))).data()?.items?.[data.itemId];
  const threads={...value.threads},thread=threads[data.itemId];
  if(!thread&&!item)fail('not-found','This shopping item is no longer available.');
  const members=Object.entries(home.members).map(([id,name])=>({id,name,color:home.memberColors?.[id]||null}));
  const targetId=thread?.targetId||(Object.hasOwn(home.members,item?.AddedBy)?item.AddedBy:null);
  if(data.action==='read')return{title:item?.Name||thread.title,targetId,members,canClear:canManage(home,uid),messages:(thread?.messages||[]).map(m=>({...m,authorName:home.members[m.authorId]||'Former member'}))};
  if(data.action==='clear'){
   if(!canManage(home,uid))fail('permission-denied','Only an adult can clear a conversation.');
   delete threads[data.itemId];tx.set(ref,{...value,threads});return{};
  }
  if(typeof data.messageId!=='string'||!/^[a-f0-9]{32}$/.test(data.messageId)||typeof data.text!=='string'||!data.text.trim()||data.text.length>1000)fail('invalid-argument','Enter a message of 1–1,000 characters.');
  const duplicate=thread?.messages.find(m=>m.id===data.messageId);
  if(duplicate){if(duplicate.authorId!==uid||duplicate.text!==data.text.trim())fail('invalid-argument','Message identifier already used.');return{};}
  const recipient=targetId&&targetId!==uid&&Object.hasOwn(home.members,targetId)?targetId:data.recipientId;
  if(!thread&&(typeof recipient!=='string'||recipient===uid||!Object.hasOwn(home.members,recipient)))fail('invalid-argument','Choose another household member to ask.');
  const now=Date.now(),day=new Date(now).toISOString().slice(0,10),rate=value.rates?.[uid];
  if(rate?.day===day&&rate.count>=60)fail('resource-exhausted','You have reached today’s household message limit.');
  const participants=[...new Set([...(thread?.participants||[]),uid,...(recipient?[recipient]:[])])].filter(id=>Object.hasOwn(home.members,id));
  const messages=[...(thread?.messages||[]),{id:data.messageId,authorId:uid,text:data.text.trim(),createdAt:now}].slice(-40);
  threads[data.itemId]={title:item?.Name||thread.title,targetId:thread?.targetId||recipient,participants,messages};
  const next={threads,rates:{...value.rates,[uid]:{day,count:(rate?.day===day?rate.count:0)+1}}};
  if(Object.keys(threads).length>50||Buffer.byteLength(JSON.stringify(next))>500000)fail('resource-exhausted','Household conversations are full. Ask an adult to clear an old conversation.');
  tx.set(ref,next);
  for(const recipientId of participants.filter(id=>id!==uid))notify(tx,db,recipientId,`shopping:${data.householdId}:${data.itemId}:${data.messageId}:${recipientId}`,{type:'householdQuestion',message:'A household shopping question or reply is waiting.',householdId:data.householdId,itemId:data.itemId},now);
  return{};
 });
};}
module.exports={createShoppingQuestions,forgetQuestions};
