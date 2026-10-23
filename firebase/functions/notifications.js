const {createHash}=require('node:crypto');
const {fail}=require('./domain');
const {requestData}=require('./request');
// Stable event IDs make retries safe. Inbox data is written only by trusted handlers.
function notify(tx,db,recipient,event,body,now){
  const key=createHash('sha256').update(event).digest('hex');
  tx.set(db.doc(`notifications/${recipient}/items/${key}`),{...body,createdAt:now,read:false,deleteAfter:new Date(now+90*86400000)});
}
function createNotifications(db,{socialEnabled=true}={}){return async request=>{
  if(!request.auth)fail('unauthenticated','Sign in to view your activity.');
  const data=requestData(request,['list','read','registerDevice','unregisterDevice']);const items=db.collection(`notifications/${request.auth.uid}/items`);
  if(data.action==='registerDevice'||data.action==='unregisterDevice'){
    if(typeof data.token!=='string'||data.token.length<20||data.token.length>4096||/\s/.test(data.token))fail('invalid-argument','Invalid notification device.');
    const ref=db.doc(`notificationDevices/${createHash('sha256').update(data.token).digest('hex')}`);
    return db.runTransaction(async tx=>{const before=(await tx.get(ref)).data();if(data.action==='registerDevice')tx.set(ref,{uid:request.auth.uid,token:data.token,updatedAt:Date.now()});else if(before?.uid===request.auth.uid)tx.delete(ref);return{};});
  }
  if(data.action==='list'){
    const docs=await (socialEnabled?items:items.where('type','==','householdQuestion')).orderBy('createdAt','desc').limit(50).get();
    const membership=(await db.doc(`householdAccounts/${request.auth.uid}`).get()).data();
    const home=membership?.householdId?(await db.doc(`households/${membership.householdId}`).get()).data():null;
    return{items:docs.docs.filter(d=>d.data().type!=='householdQuestion'||(d.data().householdId===membership?.householdId&&Object.hasOwn(home?.members||{},request.auth.uid))).map(d=>{const n=d.data();return{id:d.id,type:n.type,message:n.message,postId:n.postId,householdId:n.householdId,itemId:n.itemId,createdAt:n.createdAt,read:n.read};})};
  }
  if(typeof data.id!=='string'||! /^[a-f0-9]{64}$/.test(data.id))fail('invalid-argument','Choose an activity item.');
  return db.runTransaction(async tx=>{const ref=items.doc(data.id);if(!(await tx.get(ref)).exists)fail('not-found','Activity unavailable.');tx.update(ref,{read:true});return{};});
};}
module.exports={notify,createNotifications};
