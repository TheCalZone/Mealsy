const {createHash}=require('node:crypto');
const {fail,requireMember}=require('./domain');
const {requireFamily}=require('./entitlements');
const {requestData}=require('./request');
const {recipe}=require('./feed');
function date(value){
  if(typeof value!=='string'||!/^\d{4}-\d{2}-\d{2}$/.test(value))fail('invalid-argument','Use a calendar date in yyyy-MM-dd format.');
  const time=Date.parse(value+'T00:00:00.000Z');
  if(!Number.isFinite(time)||new Date(time).toISOString().slice(0,10)!==value)fail('invalid-argument','Choose a valid calendar date.');return time;
}
function identifier(value,pattern=/^[A-Za-z0-9_-]{1,100}$/){if(typeof value!=='string'||!pattern.test(value))fail('invalid-argument','Invalid household or meal identifier.');return value;}
function text(value,max,required=false){if(typeof value!=='string'||value.length>max||required&&!value.trim())fail('invalid-argument','Check the meal name and note lengths.');return value.trim();}
function mealInput(value,weekStart){
  if(!value||typeof value!=='object'||Array.isArray(value))fail('invalid-argument','Choose a meal to save.');
  const time=date(value.date),start=date(weekStart);
  if(time<start||time>=start+7*86400000||!['breakfast','lunch','dinner'].includes(value.slot)||!Number.isInteger(value.servings)||value.servings<1||value.servings>1000)fail('invalid-argument','Choose a date in this week, a meal slot and 1–1,000 portions.');
  if(value.recipe!=null&&(typeof value.recipe!=='object'||Array.isArray(value.recipe)||!Array.isArray(value.recipe.ingredients)||value.recipe.ingredients.some(i=>!i||typeof i!=='object'||Array.isArray(i))))fail('invalid-argument','Choose a valid recipe snapshot.');
  const meal={id:identifier(value.id,/^[a-f0-9]{32}$/),date:value.date,slot:value.slot,title:text(value.title,160,true),servings:value.servings,note:text(value.note??'',500),recipe:recipe(value.recipe)};
  if(Buffer.byteLength(JSON.stringify(meal))>100000)fail('invalid-argument','This recipe snapshot is too large for a shared meal.');return meal;
}
const digest=value=>createHash('sha256').update(JSON.stringify(value)).digest('hex');
function createHouseholdMeals(db,auth,clock=Date.now){return async request=>{
  if(!request.auth)fail('unauthenticated','Sign in to use your household meal plan.');
  const uid=request.auth.uid,user=await auth.getUser(uid);if(user.disabled)fail('permission-denied','This account is disabled.');
  const data=requestData(request,['capabilities','readWeek','upsertMeal','deleteMeal']);
  if(data.action==='capabilities')return{version:1};
  const householdId=identifier(data.householdId);date(data.weekStart);
  const meal=data.action==='upsertMeal'?mealInput(data.meal,data.weekStart):null;
  const mealId=data.action==='deleteMeal'?identifier(data.mealId,/^[a-f0-9]{32}$/):meal?.id;
  if(data.action!=='readWeek'&&(!Number.isSafeInteger(data.expectedVersion)||data.expectedVersion<0))fail('invalid-argument','Choose a valid meal version.');
  const householdRef=db.doc(`households/${householdId}`),weekRef=householdRef.collection('mealWeeks').doc(data.weekStart);
  return db.runTransaction(async tx=>{
    const account=(await tx.get(db.doc(`householdAccounts/${uid}`))).data(),household=(await tx.get(householdRef)).data();requireMember(household,uid);
    if(account?.householdId!==householdId)fail('permission-denied','Your household changed. Reopen its meal plan.');
    await requireFamily(tx,db,household.owner,clock());
    const week=(await tx.get(weekRef)).data()||{revision:0,meals:{}};
    if(data.action==='readWeek')return{householdId,weekStart:data.weekStart,revision:week.revision,meals:Object.values(week.meals).filter(m=>!m.deleted).map(({lastMutation,...m})=>m).sort((a,b)=>a.date.localeCompare(b.date)||a.slot.localeCompare(b.slot)||a.id.localeCompare(b.id))};
    const lastMutation=digest({uid,action:data.action,mealId,meal,expectedVersion:data.expectedVersion}),previous=week.meals[mealId];
    if(previous?.lastMutation===lastMutation)return{version:previous.version};
    if(previous?.deleted||previous&&previous.version!==data.expectedVersion||!previous&&data.expectedVersion!==0)fail('aborted','Someone changed or removed this meal. Your draft is kept. Review the latest week before saving again.');
    if(data.action==='deleteMeal'&&!previous)fail('not-found','This meal no longer exists.');
    if(!previous&&Object.keys(week.meals).length>=500)fail('resource-exhausted','This week has reached its edit-history limit. Choose another week.');
    const version=(previous?.version||0)+1;
    const saved=data.action==='deleteMeal'?{id:mealId,deleted:true,version,lastMutation}:{...meal,version,updatedBy:uid,lastMutation};
    const meals={...week.meals,[mealId]:saved};
    if(Object.values(meals).filter(m=>!m.deleted).length>100||Buffer.byteLength(JSON.stringify(meals))>750000)fail('resource-exhausted','This week is full. Remove a meal or use smaller recipe attachments.');
    tx.set(weekRef,{revision:week.revision+1,meals,updatedAt:clock()});return{version};
  });
};}
module.exports={createHouseholdMeals,mealInput,date};
