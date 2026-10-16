import {initializeApp} from 'https://www.gstatic.com/firebasejs/11.10.0/firebase-app.js';
import {getAuth, onAuthStateChanged, signInWithEmailAndPassword, createUserWithEmailAndPassword, signOut} from 'https://www.gstatic.com/firebasejs/11.10.0/firebase-auth.js';
import {getFunctions, httpsCallable} from 'https://www.gstatic.com/firebasejs/11.10.0/firebase-functions.js';
import {firebaseConfig} from './firebase-config.js';
const $ = id => document.getElementById(id);
const status = message => { $('status').textContent = message; };
let auth, callable, user, cursor, draftId = crypto.randomUUID(), photo = '', moderator = false, preparingPhotos = 0;
const call = async data => {
  const uid = user?.uid; if (!uid) throw Error('Sign in first.');
  const result = await callable(data); if (user?.uid !== uid) throw Error('Your account changed. Refresh the page.'); return result.data;
};
const run = async (button, action) => {
  if (button) button.disabled = true;
  try { await action(); } catch (e) { status(e.message || 'Could not complete this action.'); }
  finally { if (button) button.disabled = false; }
};
const element = (tag, text) => { const node = document.createElement(tag); if (text != null) node.textContent = text; return node; };
const button = (text, action) => { const b = element('button', text); b.type = 'button'; b.onclick = () => run(b, action); return b; };
const draftKey = () => `mealsy-feed-draft.${user?.uid}`;
function saveDraft() {
  if (!user) return;
  try { localStorage.setItem(draftKey(), JSON.stringify({id: draftId, caption: $('caption').value, name: $('name').value, recipe: $('recipe').value, photo})); }
  catch { status('Draft storage is full. Keep this tab open until publishing.'); }
}
function restoreDraft() {
  $('composer').reset(); photo = ''; draftId = crypto.randomUUID();
  try { const d = JSON.parse(localStorage.getItem(draftKey()) || 'null'); if (!d) return;
    draftId = d.id; photo = d.photo || ''; for (const field of ['caption','name','recipe']) $(field).value = d[field] || '';
  } catch { status('The previous draft could not be read.'); }
}
async function load(reset = true) {
  if (reset) { cursor = null; $('feed').replaceChildren(); }
  const moderation = moderator && $('moderation').checked;
  const page = await call({action: moderation ? 'moderation' : 'list', cursor});
  cursor = page.cursor; $('more').hidden = !cursor;
  $('feed-title').textContent = moderation ? 'Moderation · newest first' : 'Fresh from the community';
  for (const post of page.posts) $('feed').append(render(post, moderation));
  status(page.posts.length ? 'Up to date. Refresh to see new posts.' : cursor ? 'No visible posts on this page. Load more to continue.' : 'Nothing here yet. Share the first meal.');
}
function render(post, moderation) {
  const card = element('article'); card.append(element('h3', `${post.authorName || 'Removed post'}${post.hidden ? ' · Hidden' : ''}${post.deleted ? ' · Deleted' : ''}`), element('small', new Date(post.createdAt).toLocaleString()));
  if (post.photo) { const image = element('img'); image.src = `data:image/${post.photo.startsWith('iVBOR') ? 'png' : 'jpeg'};base64,${post.photo}`; image.alt = 'Food shared by ' + post.authorName; card.append(image); }
  card.append(element('p', post.caption || ''));
  if (post.recipe) {
    const details = element('details'); details.append(element('summary', post.recipe.title), element('p', post.recipe.description || ''), element('p', `Serves ${post.recipe.servings} · Prep ${post.recipe.prepMinutes} min · Cook ${post.recipe.cookMinutes || 0} min`));
    let lastSection;
    for (const i of post.recipe.ingredients) { if ((i.section || 'Ingredients') !== lastSection) { lastSection = i.section || 'Ingredients'; details.append(element('h4', lastSection)); } details.append(element('p', `• ${i.quantity} ${i.name}${i.note ? ' — ' + i.note : ''}`)); }
    details.append(element('p', post.recipe.method)); card.append(details);
  }
  if (!post.deleted && !post.hidden) {
    card.append(button(`${post.liked ? '♥ Liked' : '♡ Like'} · ${post.likeCount}`, async () => { await call({action: 'like', postId: post.id, liked: !post.liked}); await load(); }));
    const conversation = element('div'); card.append(conversation);
    card.append(button(`Comments · ${post.commentCount}`, async () => { conversation.replaceChildren(); await comments(post.id, conversation); }));
    card.append(button('Report', async () => { const reason = prompt('Why should a moderator review this post? (500 characters maximum)'); if (reason?.trim()) { await call({action: 'report', postId: post.id, reason}); status('Report received. Thank you.'); } }));
  }
  if (!post.deleted && (post.authorId === user.uid || moderator)) card.append(button('Delete post', async () => { if (!confirm('Permanently remove this shared post? The original private recipe is kept.')) return; await call({action: 'delete', postId: post.id}); await load(); }));
  if (moderation && !post.deleted) card.append(button(post.hidden ? 'Restore post' : 'Hide post', async () => { await call({action: 'moderate', postId: post.id, hidden: !post.hidden}); await load(); }));
  for (const report of post.reports || []) {
    const row = element('div'); row.className = 'report'; row.append(element('p', report.reason), button('Mark report reviewed', async () => { await call({action: 'resolveReport', postId: post.id, reporterId: report.id}); row.remove(); })); card.append(row);
  }
  return card;
}
async function comments(postId, container) {
  const list = element('div'), more = button('Older comments', () => page(false)); let next = null;
  const name = element('input'); name.placeholder = 'Public display name'; name.maxLength = 60; name.value = $('name').value;
  const text = element('textarea'); text.placeholder = 'Add a kind comment'; text.maxLength = 1000;
  let id = crypto.randomUUID(), sentText;
  const send = button('Post comment', async () => {
    if (sentText !== text.value) { id = crypto.randomUUID(); sentText = text.value; }
    const sent = text.value;
    await call({action: 'comment', postId, id, text: sent, authorName: name.value}); if (text.value === sent) text.value = ''; id = crypto.randomUUID(); sentText = null; await page(true);
  });
  container.append(name, text, send, list, more);
  async function page(reset) {
    const response = await call({action: 'comments', postId, cursor: reset ? null : next}); if (reset) list.replaceChildren(); next = response.cursor; more.hidden = !next;
    for (const comment of response.comments) {
      const row = element('div'); row.append(element('h4', comment.authorName), element('p', comment.text));
      if (comment.authorId === user.uid || moderator) row.append(button('Remove comment', async () => { await call({action: 'deleteComment', postId, commentId: comment.id}); await page(true); })); list.append(row);
    }
  }
  await page(true);
}
async function preparePhoto(file) {
  if (file.size > 15 * 1024 * 1024) throw Error('Choose a photo smaller than 15 MB.');
  const bitmap = await createImageBitmap(file); const ratio = Math.min(1, 1024 / Math.max(bitmap.width, bitmap.height));
  const canvas = document.createElement('canvas'); canvas.width = Math.max(1, Math.round(bitmap.width * ratio)); canvas.height = Math.max(1, Math.round(bitmap.height * ratio));
  canvas.getContext('2d').drawImage(bitmap, 0, 0, canvas.width, canvas.height); bitmap.close();
  for (let q = .8; q >= .2; q -= .15) { const result = canvas.toDataURL('image/jpeg', q).split(',')[1]; if (result.length <= 200000) return result; }
  throw Error('Choose a smaller or simpler photo.');
}
$('login-form').onsubmit = e => { e.preventDefault(); run(e.submitter, () => signInWithEmailAndPassword(auth, $('email').value, $('password').value)); };
$('signup').onclick = e => run(e.target, () => createUserWithEmailAndPassword(auth, $('email').value, $('password').value));
$('logout').onclick = e => run(e.target, () => signOut(auth));
$('refresh').onclick = e => run(e.target, async () => { const token = await user.getIdTokenResult(true); moderator = token.claims.feedModerator === true; $('moderation-control').hidden = !moderator; await load(); });
$('moderation').onchange = () => run(null, () => load()); $('more').onclick = e => run(e.target, () => load(false));
for (const field of ['name','caption','recipe']) $(field).oninput = saveDraft;
$('photo').onchange = () => run(null, async () => {
  const uid = user?.uid; preparingPhotos++;
  try {
    const prepared = $('photo').files[0] ? await preparePhoto($('photo').files[0]) : '';
    if (user?.uid !== uid) return;
    photo = prepared; saveDraft(); status(photo ? 'Photo ready; location metadata removed.' : 'Photo removed.');
  } finally { preparingPhotos--; }
});
$('discard').onclick = () => {
  if (!confirm('Discard this unpublished draft?')) return;
  if (user) localStorage.removeItem(draftKey());
  const name = $('name').value; $('composer').reset(); $('name').value = name; photo = ''; draftId = crypto.randomUUID();
};
$('composer').onsubmit = e => { e.preventDefault(); run(e.submitter, async () => {
  if (preparingPhotos) throw Error('Wait for the photo to finish before publishing.');
  if (!confirm('Publish this post, recipe and photo to all signed-in Mealsy users?')) return;
  saveDraft(); const recipe = $('recipe').value.trim() ? JSON.parse($('recipe').value) : null;
  const controls = [...$('composer').querySelectorAll('input,textarea,button')]; controls.forEach(c => c.disabled = true);
  try {
    await call({action: 'publish', id: draftId, caption: $('caption').value, authorName: $('name').value, recipe, photo});
    localStorage.removeItem(draftKey()); const name = $('name').value; $('composer').reset(); $('name').value = name; photo = ''; draftId = crypto.randomUUID(); await load();
  } finally { controls.forEach(c => c.disabled = false); }
}); };
if (firebaseConfig.apiKey.startsWith('ADD_')) { $('login-form').hidden = true; status('Desktop setup: copy your public Firebase web app configuration into firebase-config.js, then redeploy Hosting. See firebase/FEED.md.'); }
else {
  const app = initializeApp(firebaseConfig); auth = getAuth(app); callable = httpsCallable(getFunctions(app, 'us-central1'), 'foodFeed');
  onAuthStateChanged(auth, async current => {
    user = current; $('feed').replaceChildren(); $('moderation').checked = false; cursor = null;
    $('login').hidden = !!user; $('signed-in').hidden = !user; $('password').value = '';
    if (!user) { status('Sign in with your Mealsy account.'); return; }
    $('identity').textContent = user.email; restoreDraft();
    await run(null, async () => { const token = await current.getIdTokenResult(); if (user !== current) return; moderator = token.claims.feedModerator === true; $('moderation-control').hidden = !moderator; await load(); });
  });
}
