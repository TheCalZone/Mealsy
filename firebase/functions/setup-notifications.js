// Trusted deployment setup only. Configures sending permissions; sends no messages.
async function main(){
 const project=process.argv[2];if(!project)throw Error('Usage: node setup-notifications.js PROJECT_ID');
 await require('firebase-tools').apps.list('ANDROID',{project,nonInteractive:true});
 for(const api of ['fcm.googleapis.com','firebaseinstallations.googleapis.com'])await require('firebase-tools/lib/ensureApiEnabled').ensure(project,api,'notifications');
 const fn=await require('firebase-tools/lib/gcp/cloudfunctionsv2').getFunction(project,'europe-west1','deliverActivity');const email=fn.serviceConfig?.serviceAccountEmail;if(!email)throw Error('The deployed notification runtime service account was not found.');
 const client=new(require('firebase-tools/lib/apiv2').Client)({urlPrefix:'https://cloudresourcemanager.googleapis.com',apiVersion:'v1',auth:true});
 const policy=(await client.post(`projects/${project}:getIamPolicy`,{options:{requestedPolicyVersion:3}})).body;
 policy.bindings||=[];let binding=policy.bindings.find(b=>b.role==='roles/firebasecloudmessaging.admin'&&!b.condition);
 if(!binding){binding={role:'roles/firebasecloudmessaging.admin',members:[]};policy.bindings.push(binding);}
 const member=`serviceAccount:${email}`;if(!binding.members.includes(member)){binding.members.push(member);await client.post(`projects/${project}:setIamPolicy`,{policy});}
 console.log('Messaging APIs enabled and deployed notification runtime authorized. No messages sent.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
