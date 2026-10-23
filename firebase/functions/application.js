const {randomBytes, createHash} = require('node:crypto');
const {fail, applyChanges, requireMember, requireOwner,contributor,anonymizeContributor} = require('./domain');
const {requireFamily, active,memberLimit} = require('./entitlements');
const {requestData} = require('./request');
const {role,canManage,queue,restrictedShopping,forget}=require('./household-requests');
const hash = token => createHash('sha256').update(token).digest('hex');
function createHandler(db) {
return async function handle(request) {
  if (!request.auth) fail('unauthenticated', 'Sign in to use household lists.');
  const uid = request.auth.uid;
  const data = requestData(request, ['create', 'join', 'read', 'patch', 'status', 'leave', 'remove', 'delete', 'invite', 'revoke','setRole','requests','resolveRequest','cancelRequest','rename']);
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
      tx.update(ref, {members: {...household.members, [uid]: email},roles:{...household.roles,[uid]:invitation.role==='restricted'?'restricted':'adult'}});
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
        canRenameHousehold:true,permissionsVersion:1,memberLimit:memberLimit(seats),role:role(household,uid),pendingRequests:Object.values(household.requests||{}).filter(r=>r.status==='pending').length,members: Object.entries(household.members).map(([id, label]) => ({id, label,role:role(household,id)}))};
    }
    if (['read', 'patch'].includes(data.action)) {
      if (data.scope !== 'household' || data.householdId !== ref.id) fail('invalid-argument', 'Choose a list.');
      await requireFamily(tx, db, household.owner);
      const listRef = ref.collection('lists').doc('shopping');
      const current = (await tx.get(listRef)).data()?.items || {};
      if(data.action==='patch'&&!canManage(household,uid)){
        const result=restrictedShopping(household,uid,current,data.changes);
        tx.set(listRef,{items:result.items});
        tx.update(ref,{requests:result.requests,requestRates:household.requestRates||{}});
        return{items:Object.values(result.items),requestedCount:result.requestedCount};
      }
      const items = data.action === 'patch' ? applyChanges(current, data.changes,contributor(household,uid)) : current;
      if (data.action === 'patch') tx.set(listRef, {items});
      return {items: Object.values(items)};
    }
    if(['requests','resolveRequest','cancelRequest'].includes(data.action)){
      await requireFamily(tx,db,household.owner);
      const requests={...household.requests};
      if(data.action==='requests')return{requests:Object.values(requests).map(r=>({...r,authorName:household.members[r.authorId]||'Former member'})).sort((a,b)=>b.createdAt-a.createdAt),canApprove:canManage(household,uid)};
      if(typeof data.requestId!=='string'||!/^[a-f0-9]{32}$/.test(data.requestId))fail('invalid-argument','Choose a valid request.');
      const entry=requests[data.requestId];if(!entry)fail('not-found','This request no longer exists.');
      const cancel=data.action==='cancelRequest';
      if(cancel?entry.authorId!==uid:!canManage(household,uid))fail('permission-denied','Only adults can approve or decline household requests.');
      if(!cancel&&!['approve','decline'].includes(data.decision))fail('invalid-argument','Choose approve or decline.');
      if(entry.status!=='pending')return{status:entry.status};
      if(!cancel&&data.decision==='approve'){
        if(!Object.hasOwn(household.members,entry.authorId))fail('failed-precondition','The requester has left. Decline this request instead.');
        if(entry.kind==='shopping'){
          const listRef=ref.collection('lists').doc('shopping'),current=(await tx.get(listRef)).data()?.items||{};
          const items=applyChanges(current,[{before:null,after:entry.item}],contributor(household,entry.authorId,'requested'));tx.set(listRef,{items});
        }else if(entry.kind==='meal'){
          const weekRef=ref.collection('mealWeeks').doc(entry.weekStart),week=(await tx.get(weekRef)).data()||{revision:0,meals:{}};
          if(week.meals[entry.meal.id])fail('aborted','This planned meal already exists. Refresh before approving.');
          const meals={...week.meals,[entry.meal.id]:{...entry.meal,version:1,updatedBy:uid}};
          if(Object.keys(meals).length>500||Object.values(meals).filter(m=>!m.deleted).length>100||Buffer.byteLength(JSON.stringify(meals))>750000)fail('resource-exhausted','This meal week is full.');
          tx.set(weekRef,{revision:week.revision+1,meals,updatedAt:Date.now()});
        }else fail('invalid-argument','Invalid request type.');
      }
      const status=cancel?'cancelled':data.decision==='approve'?'approved':'declined';
      requests[entry.id]={...entry,status,resolvedBy:uid,resolvedAt:Date.now()};tx.update(ref,{requests});return{status};
    }
    if(data.action==='setRole'){
      requireOwner(household,uid);
      if(typeof data.member!=='string'||data.member===household.owner||!Object.hasOwn(household.members,data.member)||!['adult','restricted'].includes(data.role))fail('invalid-argument','Choose another member and a supported role.');
      tx.update(ref,{roles:{...household.roles,[data.member]:data.role}});return{};
    }
    if (data.action === 'leave') {
      if (household.owner === uid) fail('failed-precondition', 'The owner can delete the household, or remove other members.');
      const listRef=ref.collection('lists').doc('shopping'),list=(await tx.get(listRef)).data();
      if(list)tx.set(listRef,{...list,items:anonymizeContributor(list.items,uid)});
      const members = {...household.members}; delete members[uid];
      tx.update(ref, {members,...forget(household,uid)}); tx.delete(accountRef); return {};
    }
    requireOwner(household, uid);
    if (data.action === 'rename') {
      if(typeof data.name!=='string'||!data.name.trim()||data.name.trim().length>80) fail('invalid-argument','Enter a household name of 1–80 characters.');
      const name=data.name.trim();tx.update(ref,{name});return {name};
    }
    if (data.action === 'remove') {
      const member = String(data.member || '');
      if (member === uid || !Object.hasOwn(household.members, member)) fail('invalid-argument', 'Choose another household member.');
      const listRef=ref.collection('lists').doc('shopping'),list=(await tx.get(listRef)).data();
      if(list)tx.set(listRef,{...list,items:anonymizeContributor(list.items,member)});
      const members = {...household.members}; delete members[member];
      tx.update(ref, {members,...forget(household,member)}); tx.delete(db.doc(`householdAccounts/${member}`)); return {};
    }
    if (data.action === 'delete') {
      for (const member of Object.keys(household.members)) tx.delete(db.doc(`householdAccounts/${member}`));
      if (household.inviteHash) tx.delete(db.doc(`householdInvites/${household.inviteHash}`));
      tx.delete(ref.collection('lists').doc('shopping')); tx.delete(ref); return {};
    }
    if (data.action === 'invite') {
      await requireFamily(tx, db, household.owner);
      if(data.role!=null&&!['adult','restricted'].includes(data.role))fail('invalid-argument','Choose an invitation role.');
      const code = randomBytes(24).toString('hex');
      // One outstanding invitation per household. New code revokes its predecessor.
      if (household.inviteHash) tx.delete(db.doc(`householdInvites/${household.inviteHash}`));
      const inviteHash = hash(code);
      tx.create(db.doc(`householdInvites/${inviteHash}`), {householdId: ref.id, role:data.role||'adult',expires: Date.now() + 7 * 86400000});
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
