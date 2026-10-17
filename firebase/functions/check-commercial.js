// Read-only production readiness check. Never outputs tokens, receipts or user data.
async function main(){
 const project=process.argv[2];if(!project)throw Error('Usage: node check-commercial.js PROJECT_ID');
 await require('firebase-tools').apps.list('ANDROID',{project,nonInteractive:true});
 const client=new(require('firebase-tools/lib/apiv2').Client)({urlPrefix:'https://firestore.googleapis.com',apiVersion:'v1',auth:true});
 try{const r=await client.get(`projects/${project}/databases/(default)/documents/commercialConfig/campaigns`);console.log(JSON.stringify({promotionsEnabled:r.body.fields?.enabled?.booleanValue===true}));}
 catch(e){if(e.status===404||e.context?.response?.statusCode===404){console.log(JSON.stringify({promotionsEnabled:false,reason:'No activation configuration'}));}else throw e;}
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
