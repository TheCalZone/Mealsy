const {test,before,after}=require('node:test');
const assert=require('node:assert/strict');
const {readFileSync}=require('node:fs');
const http=require('node:http');
const {createRequire}=require('node:module');
const {chromium}=createRequire(`${__dirname}/functions/package.json`)('playwright-core');
let server,browser,origin;
before(async()=>{
 server=http.createServer((req,res)=>{
  const pathname=new URL(req.url,'http://local').pathname;
  const file=pathname==='/share/'?'/share/index.html':pathname==='/'?'/index.html':pathname;
  if(!['/share/index.html','/share/share.js','/share/share.css','/share/card.png','/product-features.js','/studio-loader.js','/index.html'].includes(file)){res.writeHead(404).end();return;}
  for(const rule of JSON.parse(readFileSync(`${__dirname}/../firebase.json`)).hosting.headers||[])if(rule.source==='**')for(const h of rule.headers)res.setHeader(h.key,h.value);
  res.setHeader('Content-Type',file.endsWith('.js')?'text/javascript':file.endsWith('.css')?'text/css':file.endsWith('.png')?'image/png':'text/html');
  res.end(readFileSync(`${__dirname}/desktop${file}`));
 });
 await new Promise(r=>server.listen(0,'127.0.0.1',r));origin=`http://127.0.0.1:${server.address().port}`;
 browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',headless:true,args:['--no-sandbox']});
});
after(async()=>{if(browser)await browser.close();if(server)await new Promise(r=>server.close(r));});
test('paused social links cannot open posts or creator plans from the landing page',async()=>{
 for(const kind of ['post','social','plan']){
  const page=await browser.newPage({viewport:{width:390,height:844}});const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto(`${origin}/share/?kind=${kind}&id=recipe_123`);
  assert.equal(await page.locator('#open').getAttribute('href'),'mealsy://welcome');
  assert.equal(await page.locator('#copy').isVisible(),false);
  assert.match(await page.locator('#status').textContent(),/currently unavailable/);
  assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true);
  assert.deepEqual(errors,[]);await page.close();
 }
});
test('malformed sharing links cannot create actionable content links while social is paused',async()=>{
 for(const query of ['?kind=admin&id=x','?kind=post&id=../users','?kind=post&id=x&kind=social','?kind=post','?kind=post&id=x#secret','?kind=post&id=x&redirect=https://evil.test']){
  const page=await browser.newPage();await page.goto(`${origin}/share/${query}`);
  assert.equal(await page.locator('#open').getAttribute('href'),'mealsy://welcome');assert.equal(await page.locator('#copy').isVisible(),false);
  await page.close();
 }
});
test('desktop community studio redirects without loading its Firebase application',async()=>{
 const page=await browser.newPage();const requests=[];page.on('request',r=>requests.push(r.url()));await page.goto(origin+'/');await page.waitForURL(origin+'/share/');
 assert.equal(requests.some(url=>url.endsWith('/app.js')),false);await page.close();
});
test('generic landing page preserves app download and first-install return guidance',async()=>{
 const page=await browser.newPage();await page.goto(`${origin}/share/`);
 assert.equal(await page.locator('#open').getAttribute('href'),'mealsy://welcome');
 assert.equal(await page.locator('#copy').isVisible(),false);
 assert.match(await page.locator('body').textContent(),/Closed-test users need the tester opt-in link/);
 await page.close();
});
