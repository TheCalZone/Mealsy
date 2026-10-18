const {fail}=require('./domain');
// The deliberate release pause applies to old clients as well as the current UI.
const socialEnabled=false;
function socialHandler(handler){return async request=>{
  if(!request.auth)fail('unauthenticated','Sign in to use Mealsy.');
  if(!socialEnabled)fail('failed-precondition','Community and creator features are currently paused. Your saved data is retained.');
  return handler(request);
};}
module.exports={socialEnabled,socialHandler};
