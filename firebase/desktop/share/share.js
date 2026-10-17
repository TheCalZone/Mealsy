'use strict';
const params=new URLSearchParams(location.search);
const kind=params.get('kind'), id=params.get('id');
const valid=['post','social','plan'].includes(kind) && /^[A-Za-z0-9_-]{1,100}$/.test(id||'') && [...params.keys()].length===2 && params.getAll('kind').length===1 && params.getAll('id').length===1 && !location.hash;
const open=document.getElementById('open'), status=document.getElementById('status'), copy=document.getElementById('copy');
if(valid){
  document.getElementById('shared-title').textContent=kind==='plan'?'A meal plan worth making room for':'Your shared post is waiting in Mealsy';
  open.href=`mealsy://content?kind=${encodeURIComponent(kind)}&id=${encodeURIComponent(id)}`;
  open.textContent=kind==='plan'?'Open meal plan in Mealsy':'Open post in Mealsy';
  copy.hidden=false;
  copy.onclick=async()=>{
    const link=`https://mealsy-b732b.web.app/share/?kind=${encodeURIComponent(kind)}&id=${encodeURIComponent(id)}`;
    try{await navigator.clipboard.writeText(link);status.textContent='Link copied. After installing, return here or send the link to Mealsy using your phone’s share menu.';}
    catch{status.textContent='Copy this page’s address from your browser, then return after installing Mealsy.';}
  };
}else if(location.search || location.hash){
  open.hidden=true;
  status.textContent='This sharing link is incomplete or invalid. Ask the sender to share it again from Mealsy. You can still get the app below.';
}
