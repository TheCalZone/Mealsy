const {test}=require('node:test');const assert=require('node:assert/strict');
const {protect}=require('../callable-errors');const {DomainError}=require('../domain');
test('operational failures have a correlation reference without logging payloads or raw errors',async()=>{
  const logs=[];const logger={error:(...args)=>logs.push(args),info:(...args)=>logs.push(args)};
  const sensitive='private@example.com password=secret recipe=private';
  const fn=protect(async()=>{throw Error(sensitive);},'foodFeed',logger);
  await assert.rejects(fn({data:{action:'publish',caption:sensitive},auth:{uid:sensitive}}),error=>{
    assert.equal(error.code,'internal');assert.match(error.details.reference,/^[a-f0-9]{32}$/);assert.equal(error.details.reason,'SERVICE_FAILURE');
    assert.equal(logs[0][1].reference,error.details.reference);assert(!JSON.stringify(error).includes(sensitive));return true;
  });
  assert(!JSON.stringify(logs).includes(sensitive));
});
test('safety rejection is distinguished from outages and normal validation stays out of error logs',async()=>{
  const logs=[];const logger={error:(...args)=>logs.push(['error',...args]),info:(...args)=>logs.push(['info',...args])};
  for(const [reason,code,level] of [['SAFETY_REJECTED','permission-denied','info'],['SAFETY_TIMEOUT','unavailable','error']]){
    const fn=protect(async()=>{throw new DomainError(code,'Safe message',{reason});},'foodFeed',logger);
    await assert.rejects(fn({data:{action:'publish'}}),error=>error.details.reason===reason);assert.equal(logs.at(-1)[0],level);
  }
  const count=logs.length;await assert.rejects(protect(async()=>{throw new DomainError('invalid-argument','Choose a name');},'foodFeed',logger)({data:{action:'list'}}));
  assert.equal(logs.length,count);
});
