const {test,mock}=require('node:test');const assert=require('node:assert/strict');
const {GoogleAuth}=require('google-auth-library');const {moderate}=require('../moderation');
test('moderation outages, busy checks and invalid responses have safe machine-readable reasons',async()=>{
  const previous=process.env.GCLOUD_PROJECT;process.env.GCLOUD_PROJECT='demo-mealsy';
  let response,problem;
  const stub=mock.method(GoogleAuth.prototype,'getClient',async()=>({request:async()=>{if(problem)throw problem;return{data:response};}}));
  try {
    for(const [error,reason] of [[{response:{status:429}},'SAFETY_BUSY'],[{code:'ETIMEDOUT'},'SAFETY_TIMEOUT'],[{response:{status:403}},'SAFETY_UNAVAILABLE']]){
      problem=error;await assert.rejects(moderate({caption:'private text'}),e=>e.code==='unavailable'&&e.details.reason===reason);
    }
    problem=null;response={};await assert.rejects(moderate({caption:'private text'}),e=>e.details.reason==='SAFETY_RESPONSE_INVALID');
    response={promptFeedback:{blockReason:'SAFETY'}};await assert.rejects(moderate({caption:'private text'}),e=>e.code==='permission-denied'&&e.details.reason==='SAFETY_REJECTED');
  } finally {stub.mock.restore();if(previous===undefined)delete process.env.GCLOUD_PROJECT;else process.env.GCLOUD_PROJECT=previous;}
});
