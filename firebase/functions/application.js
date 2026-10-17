const {randomBytes, createHash} = require('node:crypto');
const {fail, applyChanges, requireMember, requireOwner} = require('./domain');
const {requireFamily, active,memberLimit} = require('./entitlements');
const {requestData} = require('./request');
const hash = token => createHash('sha256').update(token).digest('hex');
function createHandler(db) {
return async function handle(request) {
  if (!request.auth) fail('unauthenticated', 'Sign in to use household lists.');
  const uid = request.auth.uid;
  const data = requestData(request, ['create', 'join', 'read', 'patch', 'status', 'leave', 'remove', 'delete', 'invite', 'revoke']);
  if (['read', 'patch'].includes(data.action) && !['personal', 'household'].includes(data.scope))
    fail('invalid-argument', 'Choose a list.');
  const accountRef = db.doc(`householdAccounts/${uid}`);
  const email = request.auth.token.email || 'Family member';
  if (data.action === 'create') {
    if (typeof data.name !== 'string') fail('invalid-argument', 'Choose a household name.');
    const name = data.name.trim();
    if (!name || name.length > 80) fail('invalid-argument', 'Choose a household name (up to 80 characters).');
    const ref = db.collection('households').doc();
    await db.runTransaction(async tx => {
      if ((await tx.get(accountRef)).exists) fail('failed-precondition', 'Leave your current household first.');
      await requireFamily(tx, db, uid);
      tx.create(ref, {name, owner: uid, members: {[uid]: email}});
      tx.create(accountRef, {householdId: ref.id});
    });
    return {householdId: ref.id};
  }
  if (data.action === 'join') {
    if (typeof data.code !== 'string') fail('invalid-argument', 'Paste the complete invitation code.');
    const code = data.code.trim();
    if (!/^[a-f0-9]{48}$/i.test(code)) fail('invalid-argument', 'Paste the complete invitation code.');
    const inviteRef = db.doc(`householdInvites/${hash(code.toLowerCase())}`);
    return db.runTransaction(async tx => {
      const [account, invite] = await Promise.all([tx.get(accountRef), tx.get(inviteRef)]);
      if (account.exists) fail('failed-precondition', 'Leave your current household first.');
      const invitation = invite.data();
      if (!invitation || invitation.expires <= Date.now()) fail('not-found', 'This invitation has expired or has already been used.');
      const ref = db.doc(`households/${invitation.householdId}`);
      const household = (await tx.get(ref)).data();
      if (!household) fail('not-found', 'This household no longer exists.');
      await requireFamily(tx, db, household.owner);
      const limit=memberLimit((await tx.get(db.doc(`seatEntitlements/${household.owner}`))).data());
      if (Object.keys(household.members).length >= limit) fail('resource-exhausted', `Your household includes ${limit} people. Extra paid places must be active before inviting more.`);
      tx.update(ref, {members: {...household.members, [uid]: email}});
      tx.create(accountRef, {householdId: ref.id});
      tx.delete(inviteRef); // Single use, including concurrent acceptance.
      return {householdId: ref.id};
    });
  }
  if (['read', 'patch'].includes(data.action) && data.scope === 'personal') {
    const ref = db.doc(`shoppingLists/${uid}`);
    return db.runTransaction(async tx => {
      const current = (await tx.get(ref)).data()?.items || {};
      const items = data.action === 'patch' ? applyChanges(current, data.changes) : current;
      if (data.action === 'patch') tx.set(ref, {items});
      return {items: Object.values(items)};
    });
  }
  // Membership is rechecked inside every transaction, including list edits/removal races.
  return db.runTransaction(async tx => {
    const account = (await tx.get(accountRef)).data();
    if (!account) {
      if (data.action === 'status') return {householdId: null, members: []};
      fail('failed-precondition', 'Create or join a household first.');
    }
    const ref = db.doc(`households/${account.householdId}`);
    const household = (await tx.get(ref)).data();
    requireMember(household, uid);
    if (data.householdId && data.householdId !== ref.id) fail('permission-denied', 'The household changed. Reopen its list.');
    if (data.action === 'status') {
      const plan = (await tx.get(db.doc(`billingEntitlements/${household.owner}`))).data();
      const grant = (await tx.get(db.doc(`familyGrants/${household.owner}`))).data();
      const seats=(await tx.get(db.doc(`seatEntitlements/${household.owner}`))).data();
      return {householdId: ref.id, name: household.name, owner: household.owner, familyEnabled: active(plan) || active(grant),
        memberLimit:memberLimit(seats),members: Object.entries(household.members).map(([id, label]) => ({id, label}))};
    }
    if (['read', 'patch'].includes(data.action)) {
      if (data.scope !== 'household' || data.householdId !== ref.id) fail('invalid-argument', 'Choose a list.');
      await requireFamily(tx, db, household.owner);
      const listRef = ref.collection('lists').doc('shopping');
      const current = (await tx.get(listRef)).data()?.items || {};
      const items = data.action === 'patch' ? applyChanges(current, data.changes) : current;
      if (data.action === 'patch') tx.set(listRef, {items});
      return {items: Object.values(items)};
    }
    if (data.action === 'leave') {
      if (household.owner === uid) fail('failed-precondition', 'The owner can delete the household, or remove other members.');
      const members = {...household.members}; delete members[uid];
      tx.update(ref, {members}); tx.delete(accountRef); return {};
    }
    requireOwner(household, uid);
    if (data.action === 'remove') {
      const member = String(data.member || '');
      if (member === uid || !Object.hasOwn(household.members, member)) fail('invalid-argument', 'Choose another household member.');
      const members = {...household.members}; delete members[member];
      tx.update(ref, {members}); tx.delete(db.doc(`householdAccounts/${member}`)); return {};
    }
    if (data.action === 'delete') {
      for (const member of Object.keys(household.members)) tx.delete(db.doc(`householdAccounts/${member}`));
      if (household.inviteHash) tx.delete(db.doc(`householdInvites/${household.inviteHash}`));
      tx.delete(ref.collection('lists').doc('shopping')); tx.delete(ref); return {};
    }
    if (data.action === 'invite') {
      await requireFamily(tx, db, household.owner);
      const code = randomBytes(24).toString('hex');
      // One outstanding invitation per household. New code revokes its predecessor.
      if (household.inviteHash) tx.delete(db.doc(`householdInvites/${household.inviteHash}`));
      const inviteHash = hash(code);
      tx.create(db.doc(`householdInvites/${inviteHash}`), {householdId: ref.id, expires: Date.now() + 7 * 86400000});
      tx.update(ref, {inviteHash}); return {code};
    }
    if (data.action === 'revoke') {
      if (household.inviteHash) tx.delete(db.doc(`householdInvites/${household.inviteHash}`));
      tx.update(ref, {inviteHash: null}); return {};
    }
    fail('invalid-argument', 'Unknown household action.');
  });
};
}
module.exports = {createHandler};
