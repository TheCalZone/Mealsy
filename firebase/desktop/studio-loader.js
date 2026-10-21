'use strict';
if(window.MealsyRelease?.socialEnabled===true){
  document.body.hidden=false;
  const studio=document.createElement('script');studio.type='module';studio.src='/app.js';document.body.append(studio);
}else location.replace('/share/');
