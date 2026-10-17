const {test, before, after} = require('node:test');
const assert = require('node:assert/strict');
const {readFileSync, existsSync} = require('node:fs');
const http = require('node:http');
const {createRequire} = require('node:module');
const localRequire = createRequire(`${__dirname}/functions/package.json`);
const {chromium} = localRequire('playwright-core');
let server, browser, origin;
before(async () => {
  server = http.createServer((req, res) => {
    const path = req.url === '/' ? '/index.html' : req.url;
    if (!['/index.html','/app.js','/style.css','/firebase-config.js'].includes(path)) { res.writeHead(404).end(); return; }
    for (const rule of JSON.parse(readFileSync(`${__dirname}/../firebase.json`, 'utf8')).hosting.headers || []) {
      if (rule.source === '**') for (const header of rule.headers) res.setHeader(header.key, header.value);
    }
    res.setHeader('Content-Type', path.endsWith('.js') ? 'text/javascript' : path.endsWith('.css') ? 'text/css' : 'text/html');
    res.end(readFileSync(`${__dirname}/desktop${path}`));
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve)); origin = `http://127.0.0.1:${server.address().port}`;
  const executablePath = process.env.CHROMIUM_PATH || '/usr/bin/chromium';
  assert(existsSync(executablePath), 'Set CHROMIUM_PATH to an installed Chrome/Chromium executable.');
  browser = await chromium.launch({executablePath, headless: true, args: ['--no-sandbox']});
});
after(async () => { if (browser) await browser.close(); if (server) await new Promise(resolve => server.close(resolve)); });
async function page(moderator = false, configured = true, hosted = false) {
  const page = await browser.newPage();
  await page.addInitScript(({moderator}) => {
    window.calls = [];
    window.testUser = {uid: 'alice', email: 'alice@example.com', getIdTokenResult: async () => ({claims: {feedModerator: moderator}})};
    window.post = {id: 'post-1', authorName: 'Chef', authorId: 'bob', createdAt: 1000000, caption: '<img src=x onerror="window.injected=true"> Delicious dinner!', photo: '', recipe: null, likeCount: 0, commentCount: 0, liked: false};
    window.mockCall = async data => {
      window.calls.push(data);
      if (data.action === 'list') return {data: {posts: [window.post], cursor: null}};
      if (data.action === 'moderation') return {data: {posts: [{...window.post, hidden: true, reports: [{id: 'reporter', reason: 'Please review'}]}], cursor: null}};
      if (data.action === 'like') { window.post.liked = data.liked; window.post.likeCount = data.liked ? 1 : 0; }
      if (data.action === 'comments') return {data: {comments: [], cursor: null}};
      return {data: {id: data.id}};
    };
  }, {moderator});
  if (hosted) await page.route('**/__/firebase/init.json', route => route.fulfill({contentType: 'application/json', body: JSON.stringify({apiKey: 'hosted-test-key', appId: 'hosted-test-app', projectId: hosted === true ? 'mealsy-b732b' : 'another-project', authDomain: 'mealsy-b732b.firebaseapp.com'})}));
  const modules = {
    'firebase-app.js': 'export const initializeApp = config => config;',
    'firebase-auth.js': `export const getAuth = () => ({}); export const onAuthStateChanged = (auth, callback) => {window.authChanged = callback; queueMicrotask(() => callback(window.testUser));}; export const signInWithEmailAndPassword = async () => {}; export const createUserWithEmailAndPassword = async () => {}; export const signOut = async () => window.authChanged(null);`,
    'firebase-functions.js': 'export const getFunctions = () => ({}); export const httpsCallable = () => data => window.mockCall(data);'
  };
  await page.route('https://www.gstatic.com/firebasejs/**', route => route.fulfill({contentType: 'text/javascript', body: modules[route.request().url().split('/').at(-1)]}));
  if (configured) await page.route('**/firebase-config.js', route => route.fulfill({contentType: 'text/javascript', body: 'export const firebaseConfig = {apiKey:"test-public-config", projectId:"demo-mealsy"};'}));
  await page.goto(origin); return page;
}
test('desktop renders real feed UI, escapes user content and persists like actions', async () => {
  const p = await page();
  await p.getByRole('heading', {name: 'Chef', exact: true}).waitFor();
  assert.equal(await p.locator('#feed img').count(), 0); assert.equal(await p.evaluate(() => !!window.injected), false);
  await p.getByRole('button', {name: '♡ Like · 0', exact: true}).click();
  await p.getByRole('button', {name: '♥ Liked · 1', exact: true}).waitFor();
  assert((await p.evaluate(() => window.calls)).some(c => c.action === 'like' && c.postId === 'post-1' && c.liked));
  await p.setViewportSize({width: 360, height: 800});
  assert.equal(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
  await p.close();
});
test('desktop publish requires explicit confirmation and sends the shared snapshot', async () => {
  const p = await page(); await p.locator('#signed-in').waitFor({state: 'visible'});
  await p.locator('#name').fill('Home cook'); await p.locator('#caption').fill('Tonight’s pasta');
  p.once('dialog', dialog => dialog.accept()); await p.getByRole('button', {name: 'Publish to the community', exact: true}).click();
  await p.waitForFunction(() => window.calls.some(c => c.action === 'publish'));
  const call = (await p.evaluate(() => window.calls)).find(c => c.action === 'publish');
  assert.equal(call.authorName, 'Home cook'); assert.equal(call.caption, 'Tonight’s pasta'); assert.equal(call.recipe, null);
  await p.close();
});
test('desktop moderator can review reports and restore hidden posts', async () => {
  const p = await page(true); await p.locator('#moderation-control').waitFor({state: 'visible'}); await p.locator('#moderation').check();
  await p.getByText('Please review', {exact: true}).waitFor();
  await p.getByRole('button', {name: 'Restore post', exact: true}).click();
  assert((await p.evaluate(() => window.calls)).some(c => c.action === 'moderate' && c.hidden === false));
  await p.close();
});
test('desktop hides moderation for normal users and clears feed on logout', async () => {
  const p = await page(); await p.getByRole('heading', {name: 'Chef', exact: true}).waitFor();
  assert.equal(await p.locator('#moderation-control').isVisible(), false);
  await p.getByRole('button', {name: 'Sign out', exact: true}).click(); await p.locator('#login').waitFor({state: 'visible'});
  assert.equal(await p.locator('#feed article').count(), 0); await p.close();
});
test('unconfigured desktop explains the missing Firebase web setup', async () => {
  const p = await page(false, false); await p.getByRole('status').filter({hasText: 'Desktop setup:'}).waitFor(); await p.close();
});

test('desktop renders extended recipe metadata and ingredient sections safely', async () => {
  const p = await page();
  await p.getByRole('heading', {name: 'Chef', exact: true}).waitFor();
  await p.evaluate(() => { window.post.recipe = {title:'Family pasta', description:'A simple dinner', servings:4, prepMinutes:10, cookMinutes:20, method:'1. Cook.', ingredients:[{name:'Tomatoes', quantity:'400 g', section:'Sauce', note:''}]}; });
  await p.getByRole('button', {name:'♡ Like · 0', exact:true}).click();
  await p.getByText('Family pasta', {exact:true}).click();
  await p.getByText('A simple dinner', {exact:true}).waitFor();
  await p.getByText('Serves 4 · Prep 10 min · Cook 20 min', {exact:true}).waitFor();
  await p.getByRole('heading', {name:'Sauce', exact:true}).waitFor();
  await p.getByText('• 400 g Tomatoes', {exact:true}).waitFor();
  await p.close();
});

test('Firebase Hosting discovers web configuration without editing public identifiers', async () => {
  const p = await page(false, false, true);
  await p.getByRole('heading', {name: 'Chef', exact: true}).waitFor();
  assert.equal(await p.locator('#signed-in').isVisible(), true);
  await p.close();
});

test('Hosting discovery refuses configuration for a different Firebase project', async () => {
  const p = await page(false, false, 'wrong-project');
  await p.getByRole('status').filter({hasText: 'Desktop setup:'}).waitFor();
  assert.equal(await p.locator('#signed-in').isVisible(), false);
  await p.close();
});
