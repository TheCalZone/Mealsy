// Read-only deployment metadata. Does not read account records or send alerts.
async function main(){
 const project=process.argv[2];if(!project||!/^[-a-z0-9]+$/.test(project))throw Error('Usage: node check-household-deployment.js PROJECT_ID');
 await require('firebase-tools').apps.list('ANDROID',{project,nonInteractive:true});
 const {Client}=require('firebase-tools/lib/apiv2');
 const firestore=new Client({urlPrefix:'https://firestore.googleapis.com',apiVersion:'v1',auth:true});
 const parent=`projects/${project}/databases/(default)`;
 const api=new(require('firebase-tools/lib/firestore/api').FirestoreApi)();
 const indexes=await api.listIndexes(project);
 const indexStates=indexes.filter(i=>['accountDeletions','items'].some(c=>i.name.includes(`/collectionGroups/${c}/`))).map(i=>({collection:i.name.split('/collectionGroups/')[1].split('/')[0],fields:i.fields.map(f=>f.fieldPath),state:i.state}));
 const ttl=[];
 const groupIndexes=[];
 for(const [collection,fieldPath] of [['comments','authorId'],['reports','reporterId']]){
  const field=(await firestore.get(`${parent}/collectionGroups/${collection}/fields/${fieldPath}`)).body;
  groupIndexes.push({collection,field:fieldPath,ready:field.indexConfig?.indexes?.some(i=>i.queryScope==='COLLECTION_GROUP'&&i.state==='READY')===true});
 }
 for(const collection of ['accountDeletions','items']){
  const field=(await firestore.get(`${parent}/collectionGroups/${collection}/fields/deleteAfter`)).body;
  ttl.push({collection,state:field.ttlConfig?.state||'DISABLED'});
 }
 const functionsClient=new Client({urlPrefix:'https://cloudfunctions.googleapis.com',apiVersion:'v2',auth:true});
 const names=['accountDeletion','householdQuestions','household','householdMeals','administration','notifications','resumeAccountDeletions','processAccountDeletion','deliverActivity'];
 const functions=[];
 for(const name of names){
  const region=['processAccountDeletion','deliverActivity'].includes(name)?'europe-west1':'us-central1';
  const fn=(await functionsClient.get(`projects/${project}/locations/${region}/functions/${name}`)).body;
  functions.push({name,state:fn.state});
 }
 const scheduler=new Client({urlPrefix:'https://cloudscheduler.googleapis.com',apiVersion:'v1',auth:true});
 const jobs=(await scheduler.get(`projects/${project}/locations/us-central1/jobs`)).body.jobs||[];
 const recovery=jobs.filter(j=>j.name.endsWith('/firebase-schedule-resumeAccountDeletions-us-central1')).map(j=>({state:j.state,schedule:j.schedule}));
 console.log(JSON.stringify({functions,indexes:indexStates,groupIndexes,ttl,recovery},null,2));
 const requiredIndexes=[['accountDeletions','status','retryAfter'],['items','type','createdAt']];
 if(functions.some(f=>f.state!=='ACTIVE')||ttl.some(t=>t.state!=='ACTIVE')||groupIndexes.some(i=>!i.ready)||requiredIndexes.some(([c,...fields])=>!indexStates.some(i=>i.collection===c&&i.state==='READY'&&fields.every(f=>i.fields.includes(f))))||recovery.length!==1||recovery[0].state!=='ENABLED')process.exitCode=1;
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
