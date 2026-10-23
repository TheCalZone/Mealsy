const {randomUUID}=require('node:crypto');
const {HttpsError}=require('firebase-functions/v2/https');
const {DomainError}=require('./domain');
const actions=new Set(['rename','prepare','request','capabilities','readWeek','upsertMeal','deleteMeal','getPost','profile','setUsername','publish','comment','list','preferences','savePreferences','follow','unfollow','comments','delete','moderate','resolveReport','like','deleteComment','report','status','lookupUser','posts','setPostHidden','pausePromotion','setFamilyGrant','verifyEmail','audit','requestPromotion','activatePromotion','create','join','invite','remove','leave','read','patch','revoke','verify','restore','mine','updateProfile','workspace','removeMember','revokeInvite','publishRecipe','hideRecipe','saveCollection','collection','directory','publicProfile','track','analytics','saveCampaign','pauseCampaign','adminBusinesses','verifyBusiness','wallet','verifyCredit','start','prepareImpression','impression','registerDevice','unregisterDevice']);
const reasons=new Set(['SAFETY_REJECTED','SAFETY_BUSY','SAFETY_TIMEOUT','SAFETY_UNAVAILABLE','SAFETY_RESPONSE_INVALID']);
function protect(handler,service,logger=console) {
  return async request=>{
    try{return await handler(request);}
    catch(error){
      const reference=randomUUID().replaceAll('-','');
      const expected=error instanceof DomainError;
      const reason=reasons.has(error.details?.reason)?error.details.reason:expected?'REQUEST_REJECTED':'SERVICE_FAILURE';
      const operational=!expected||['internal','unavailable','deadline-exceeded','unknown'].includes(error.code);
      const record={reference,service,operation:actions.has(request.data?.action)?request.data.action:'unknown',reason,
        code:expected?error.code:'internal',errorType:['Error','TypeError','RangeError','ReferenceError','DomainError'].includes(error.name)?error.name:'Error'};
      // Never log request data, user identities, raw provider errors or responses.
      if(operational)logger.error('Operation failed',record);
      else if(reason==='SAFETY_REJECTED')logger.info('Safety submission rejected',record);
      throw new HttpsError(expected?error.code:'internal',expected?error.message:'The service could not complete this request. Please retry.',{reference,reason});
    }
  };
}
module.exports={protect};
