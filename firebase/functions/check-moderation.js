// Live provider smoke test with synthetic content only; creates no posts/users.
const assert = require('node:assert/strict');
const {moderationRequest, decision} = require('./moderation');
async function main() {
  const project=process.argv[2]; if(!project)throw Error('Usage: node check-moderation.js PROJECT_ID');
  await require('firebase-tools').apps.list('ANDROID',{project,nonInteractive:true});
  const client=new (require('firebase-tools/lib/apiv2').Client)({urlPrefix:'https://aiplatform.googleapis.com',apiVersion:'v1',auth:true});
  const photo = require('node:fs').readFileSync(require('node:path').join(__dirname, '../../RecipeApp/Resources/Images/salad.png')).toString('base64');
  for(const [content,expected] of [[{caption:'An easy vegetable soup for dinner.',authorName:'Home Cook'},true],
    [{caption:'Salad for dinner.',photo},true],
    [{caption:'I will hunt you down and kill you and your family.'},false]]) {
    const response=await client.post(`projects/${project}/locations/global/publishers/google/models/gemini-2.5-flash-lite:generateContent`,moderationRequest(content));
    assert.equal(decision(response.body),expected);console.log(expected?(content.photo?'Benign food image accepted.':'Benign food content accepted.'):'Threatening content rejected.');
  }
}
main().catch(error=>{console.error(error.message);process.exitCode=1;});
