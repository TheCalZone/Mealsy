const {fail} = require('./domain');
function active(entitlement, now = Date.now()) {
  return !!entitlement && entitlement.active === true && Number.isFinite(entitlement.expiresAt) && entitlement.expiresAt > now;
}
async function requireFamily(tx, db, owner, now = Date.now()) {
  const entitlement = (await tx.get(db.doc(`billingEntitlements/${owner}`))).data();
  if (!active(entitlement, now)) fail('permission-denied', 'An active Mealsy Family subscription held by the household owner is required.');
}
module.exports = {active, requireFamily};
