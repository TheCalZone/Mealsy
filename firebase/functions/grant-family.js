// Trusted administrator only. Uses Application Default Credentials, never app credentials.
const {initializeApp, applicationDefault} = require('firebase-admin/app');
const {getAuth} = require('firebase-admin/auth');
const {getFirestore} = require('firebase-admin/firestore');
async function main() {
  const [project, email, duration, flag] = process.argv.slice(2);
  if (!project || !email || !/^([1-9]\d{0,2}|revoke)$/.test(duration || '') || Number(duration) > 365)
    throw Error('Usage: node grant-family.js PROJECT_ID ACCOUNT_EMAIL DAYS_1_TO_365_OR_revoke');
  if (flag && flag !== '--firebase-cli') throw Error('Unknown option. Use --firebase-cli to use a trusted desktop Firebase CLI login.');
  let credential, cliFirestore;
  if (flag) {
    // CLI login remains in its own credential store; credentials never enter app assets.
    await require('firebase-tools').apps.list('ANDROID', {project, nonInteractive: true});
    credential = {getAccessToken: async () => {
      const token = await require('firebase-tools/lib/auth').getAccessToken(undefined, require('firebase-tools/lib/api').getScopes());
      return {access_token: token.access_token, expires_in: 3600};
    }};
    cliFirestore = new (require('firebase-tools/lib/apiv2').Client)({urlPrefix: 'https://firestore.googleapis.com', apiVersion: 'v1', auth: true});
  } else credential = applicationDefault();
  initializeApp({projectId: project, credential});
  const user = await getAuth().getUserByEmail(email);
  const path = `projects/${project}/databases/(default)/documents/familyGrants/${user.uid}`;
  const ref = cliFirestore ? {
    delete: () => cliFirestore.delete(path),
    set: value => cliFirestore.patch(path, {fields: {
      active: {booleanValue: value.active}, expiresAt: {integerValue: String(value.expiresAt)},
      grantedAt: {integerValue: String(value.grantedAt)}, source: {stringValue: value.source}
    }})
  } : getFirestore().doc(`familyGrants/${user.uid}`);
  if (duration === 'revoke') { await ref.delete(); console.log('Family test grant revoked.'); return; }
  const expiresAt = Date.now() + Number(duration) * 86400000;
  await ref.set({active: true, expiresAt, grantedAt: Date.now(), source: 'administrator'});
  console.log(`Family test access granted until ${new Date(expiresAt).toISOString()}. Reopen Account > Family in the app.`);
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
