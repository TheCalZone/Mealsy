const {fail}=require('./domain');
const roles={owner:['owner'],editor:['owner','editor'],campaign:['owner','campaignManager']};
function requireRole(business,uid,kind='editor',verified=false){
  if(!business||!roles[kind]?.includes(business.members?.[uid]))fail('permission-denied','Your business role does not allow this action.');
  if(verified&&business.verified!==true)fail('failed-precondition','This business needs administrator verification before publishing.');
  return business;
}
module.exports={requireRole};
