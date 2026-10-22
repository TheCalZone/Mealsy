const {GoogleAuth}=require('google-auth-library');const {createHash}=require('node:crypto');const {fail}=require('./domain');const {requireRole}=require('./business-access');const {PACKAGE,accountId}=require('./billing');
const PRODUCT='mealsy_promotion_credit_5',CREDIT_MICRO=5000000;const sha=s=>createHash('sha256').update(s).digest('hex');
const auth=new GoogleAuth({scopes:['https://www.googleapis.com/auth/androidpublisher']});
async function store(token,consume=false){const client=await auth.getClient();return(await client.request({method:consume?'POST':'GET',url:`https://androidpublisher.googleapis.com/androidpublisher/v3/applications/${PACKAGE}/purchases/products/${PRODUCT}/tokens/${encodeURIComponent(token)}${consume?':consume':''}`})).data;}
function createPromotionPurchases(db,read=token=>store(token),consume=token=>store(token,true),clock=Date.now){
 async function verify(uid,businessId,token,system=false){
  if(typeof businessId!=='string'||!/^[A-Za-z0-9_-]{1,100}$/.test(businessId)||typeof token!=='string'||token.length<10||token.length>4096)fail('invalid-argument','Invalid credit purchase.');
  if(!system)requireRole((await db.doc(`businesses/${businessId}`).get()).data(),uid,'campaign',true);
  const purchase=await read(token);if(purchase.obfuscatedExternalAccountId!==accountId(uid)||purchase.obfuscatedExternalProfileId!==sha(businessId))fail('permission-denied','This credit purchase belongs to another account or business.');
  const receiptRef=db.doc(`promotionPurchases/${sha(token)}`),walletRef=db.doc(`businessWallets/${businessId}`);
  await db.runTransaction(async tx=>{
   if((await tx.get(db.doc(`accountDeletions/${uid}`))).exists)return;
   const receipt=(await tx.get(receiptRef)).data(),wallet=(await tx.get(walletRef)).data()||{balanceMicro:0},b=(await tx.get(db.doc(`businesses/${businessId}`))).data();if(!system)requireRole(b,uid,'campaign',true);else if(!receipt)return;
   if(receipt&&(receipt.uid!==uid||receipt.businessId!==businessId))fail('permission-denied','This purchase is already assigned.');
   if(purchase.purchaseState!==0){if(receipt?.credited&&!receipt.revoked){tx.update(walletRef,{balanceMicro:wallet.balanceMicro-CREDIT_MICRO});tx.update(receiptRef,{revoked:true,verifiedAt:clock()});}return;}
   if(receipt?.credited)return;
   if(purchase.consumptionState!==0||(purchase.quantity??1)!==1)fail('failed-precondition','This purchase cannot be credited. Contact support with your store receipt.');
   tx.set(walletRef,{...wallet,balanceMicro:wallet.balanceMicro+CREDIT_MICRO});tx.create(receiptRef,{uid,businessId,token,credited:true,revoked:false,creditMicro:CREDIT_MICRO,verifiedAt:clock()});
  });
  if(purchase.purchaseState!==0){if(system)return{credited:false};fail('failed-precondition','The store has not confirmed this payment.');}
  if(purchase.consumptionState===0)await consume(token);
  return{credited:true};
 }
 return{verify,async notification(token){if(typeof token!=='string'||token.length>4096)return;const receipt=(await db.doc(`promotionPurchases/${sha(token)}`).get()).data();if(receipt)await verify(receipt.uid,receipt.businessId,token,true);}};
}
module.exports={createPromotionPurchases,PRODUCT,CREDIT_MICRO};
