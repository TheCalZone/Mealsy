// Trusted project administrator only; no credentials are copied into app assets.
async function main() {
  const project = process.argv[2];
  if (!project) throw Error('Usage: node setup-moderation.js PROJECT_ID');
  await require('firebase-tools').apps.list('ANDROID', {project, nonInteractive: true});
  await require('firebase-tools/lib/ensureApiEnabled').ensure(project, 'aiplatform.googleapis.com', 'moderation');
  const manager = new (require('firebase-tools/lib/apiv2').Client)({urlPrefix: 'https://cloudresourcemanager.googleapis.com', apiVersion: 'v1', auth: true});
  const details = (await manager.get(`projects/${project}`)).body;
  const member = `serviceAccount:${details.projectNumber}-compute@developer.gserviceaccount.com`;
  const policy = (await manager.post(`projects/${project}:getIamPolicy`, {options: {requestedPolicyVersion: 3}})).body;
  policy.bindings ||= [];
  let binding = policy.bindings.find(b => b.role === 'roles/aiplatform.user' && !b.condition);
  if (!binding) { binding = {role: 'roles/aiplatform.user', members: []}; policy.bindings.push(binding); }
  if (!binding.members.includes(member)) {
    binding.members.push(member);
    await manager.post(`projects/${project}:setIamPolicy`, {policy});
  }
  console.log('Vertex AI enabled; Functions runtime has Vertex AI User access. Moderation requests use project billing.');
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
