const {createHash} = require('node:crypto');
const {fail} = require('./domain');
const key = value => createHash('sha256').update(value.normalize('NFKC').toLocaleLowerCase('en-US')).digest('hex');
function username(value) {
  if (typeof value !== 'string' || !value.trim() || value.trim().length > 40 || /[\p{C}\n\r]/u.test(value))
    fail('invalid-argument', 'Choose a username with 1–40 visible characters.');
  return value.trim().normalize('NFC');
}
function verified(request) {
  if (request.auth.token?.email_verified !== true) fail('failed-precondition', 'Confirm your email address from Account before sharing with the community.');
}
async function identity(db, uid) {
  const profile = (await db.doc(`communityProfiles/${uid}`).get()).data();
  if (!profile?.username) fail('failed-precondition', 'Choose your public username on the Account page first.');
  return profile;
}
async function setUsername(db, uid, value, moderate) {
  const name = username(value);
  const existing = (await db.doc(`communityProfiles/${uid}`).get()).data();
  if (existing?.username === name) return existing;
  await moderate({username: name});
  return db.runTransaction(async tx => {
    const profileRef = db.doc(`communityProfiles/${uid}`), profile = (await tx.get(profileRef)).data();
    if (profile?.username === name) return profile;
    const counterRef = db.doc(`communityNames/${key(name)}`), counter = (await tx.get(counterRef)).data()?.counter || 0;
    const discriminator = String(counter + 1001);
    const result = {username: name, discriminator};
    tx.set(counterRef, {counter: counter + 1}); tx.set(profileRef, result); return result;
  });
}
function promoted(campaign, receipt, post, now) {
  return !!post.recipe && campaign?.active === true && campaign.owner === post.authorId && campaign.startsAt <= now && campaign.expiresAt > now &&
    receipt?.verified === true && receipt.revoked !== true && receipt.owner === post.authorId && receipt.postId === campaign.postId && receipt.expiresAt > now;
}
module.exports = {username, verified, identity, setUsername, promoted};
