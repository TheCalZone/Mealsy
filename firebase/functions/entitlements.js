const {fail} = require('./domain');
function active(entitlement, now = Date.now()) {
  return !!entitlement && entitlement.active === true && Number.isFinite(entitlement.expiresAt) && entitlement.expiresAt > now;
}
function memberLimit(seats,now=Date.now()) { return 4+(active(seats,now)&&Number.isSafeInteger(seats.extraSeats)?Math.max(0,Math.min(16,seats.extraSeats)):0); }
async function requireFamily(tx, db, owner, now = Date.now()) {
  const entitlement = (await tx.get(db.doc(`billingEntitlements/${owner}`))).data();
  const grant = (await tx.get(db.doc(`familyGrants/${owner}`))).data();
  if (!active(entitlement, now) && !active(grant, now)) fail('permission-denied', 'An active Mealsy Family subscription held by the household owner is required.');
}
module.exports = {active, requireFamily,memberLimit};
