const {createHash}=require('node:crypto');
const {fail,applyChanges,item}=require('./domain');
function role(household,uid){
 if(household.owner===uid)return 'owner';
 const value=household.roles?.[uid];
 return value==null?'adult':value==='adult'?'adult':'restricted';
}
const canManage=(h,u)=>['owner','adult'].includes(role(h,u));
function queue(household,uid,kind,id,payload,now=Date.now()){
 const key=createHash('sha256').update(`${uid}:${kind}:${id}`).digest('hex').slice(0,32);
 const requests={...household.requests};
 if(requests[key])return requests;
 if(Object.values(requests).filter(r=>r.status==='pending').length>=50)fail('resource-exhausted','Review pending household requests before sending more.');
 const day=new Date(now).toISOString().slice(0,10),rates={...household.requestRates},rate=rates[uid];
 if(rate?.day===day&&rate.count>=100)fail('resource-exhausted','You have reached today’s request limit.');
 rates[uid]={day,count:(rate?.day===day?rate.count:0)+1};
 requests[key]={id:key,kind,authorId:uid,createdAt:now,status:'pending',...payload};
 const finished=Object.values(requests).filter(r=>r.status!=='pending').sort((a,b)=>a.createdAt-b.createdAt);
 while((Object.keys(requests).length>100||Buffer.byteLength(JSON.stringify(requests))>600000)&&finished.length)delete requests[finished.shift().id];
 if(Buffer.byteLength(JSON.stringify(requests))>600000)fail('resource-exhausted','Requests are full. Review them or use smaller recipe attachments.');
 household.requestRates=rates;return requests;
}
function restrictedShopping(household,uid,current,changes){
 // Validate the entire mutation using the same schema/conflict checks as adults.
 applyChanges(current,changes);
 let requests={...household.requests},requestedCount=0;const checks=[];
 for(const change of changes){
  const before=change.before==null?null:item(change.before),after=change.after==null?null:item(change.after);
  if(!before&&after){
   if(after.InBasket)fail('permission-denied','Request an uncollected item first.');
   household.requests=requests;requests=queue(household,uid,'shopping',after.Id,{item:after});requestedCount++;continue;
  }
  if(!before||!after||['Id','Name','Category','Quantity','Note'].some(key=>before[key]!==after[key]))
   fail('permission-denied','Restricted members can check items or request additions, but cannot edit or delete shared items.');
  checks.push(change);
 }
 return{items:applyChanges(current,checks),requests,requestedCount};
}
function forget(household,uid){
 const roles={...household.roles};delete roles[uid];
 const requestRates={...household.requestRates};delete requestRates[uid];
 const requests=Object.fromEntries(Object.entries(household.requests||{}).filter(([,r])=>r.authorId!==uid).map(([id,r])=>[id,r.resolvedBy===uid?{...r,resolvedBy:'former-member'}:r]));
 return{roles,requestRates,requests};
}
module.exports={role,canManage,queue,restrictedShopping,forget};
