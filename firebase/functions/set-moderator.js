// Run only on a trusted desktop/server using Application Default Credentials.
// No service-account key belongs in the app, web assets, or repository.
const {initializeApp, applicationDefault} = require('firebase-admin/app');
const {getAuth} = require('firebase-admin/auth');
async function main() {
  const [projectId, uid, flag] = process.argv.slice(2);
  if (!projectId || !uid || (flag && flag !== '--remove')) throw Error('Usage: node set-moderator.js PROJECT_ID USER_UID [--remove]');
  initializeApp({projectId, credential: applicationDefault()});
  const auth = getAuth(), user = await auth.getUser(uid);
  await auth.setCustomUserClaims(uid, {...user.customClaims, feedModerator: flag !== '--remove'});
  console.log(flag === '--remove' ? 'Moderator claim removed. Existing ID tokens can remain valid for up to one hour.' : 'Moderator enabled. Sign in again or refresh the desktop studio token.');
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
