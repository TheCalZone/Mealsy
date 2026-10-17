// Trusted project administrator bootstrap; preserves all unrelated custom claims.
async function main() {
  const [project,email,mode]=process.argv.slice(2);
  if(!project||!email||!['grant','revoke'].includes(mode))throw Error('Usage: node set-admin.js PROJECT EMAIL grant|revoke');
  await require('firebase-tools').apps.list('ANDROID',{project,nonInteractive:true});
  const credential={getAccessToken:async()=>{
    const token=await require('firebase-tools/lib/auth').getAccessToken(undefined,require('firebase-tools/lib/api').getScopes());
    return{access_token:token.access_token,expires_in:3600};
  }};
  require('firebase-admin/app').initializeApp({projectId:project,credential});
  const auth=require('firebase-admin/auth').getAuth(),user=await auth.getUserByEmail(email);
  const claims={...user.customClaims};if(mode==='grant')claims.admin=true;else delete claims.admin;
  await auth.setCustomUserClaims(user.uid,claims);
  console.log(mode==='grant'?'Administrator access granted. Reopen Account in Mealsy.':'Administrator access revoked.');
}
main().catch(error=>{console.error(error.message);process.exitCode=1;});
