# Play Data safety working inventory (not a completed Console declaration)

Use the final signed release, deployed Firebase configuration and current Google
SDK disclosures to complete the actual form. Do not copy a blanket "no data
collected" answer because ads/social are disabled.

| Candidate data category | Source and purpose | Optionality/handling to verify |
|---|---|---|
| Email/name/user ID | Firebase Auth and profile; account management, sync and household membership | Account optional; name/profile fields as shown in signup. Deletion flow requires server rollout. |
| Dietary/profile details | Optional profile text; may reveal health information | Review whether form needs a health-info classification or product should omit the field. Inline notice added. |
| Photos and user-generated content | Recipe photos/content, snacks, essentials, shopping and household meal records | Photo input optional; cloud sync on account use; household members receive intentionally shared records. Photos currently embedded in records. |
| Purchase history/status | Play purchase tokens, account hash binding, verified renewal status | Needed for Premium; payment-card data handled by Google Play, not app. |
| Diagnostics/device identifiers | Opt-in Crashlytics app/device/crash metadata; Auth/Firebase/ads SDK identifiers must be reviewed independently | Collection default off is not proof that every bundled SDK collects nothing. Review traffic and provider documentation. |
| Public social content | Disabled in source; historic records might still exist | Confirm production feature gates and retained records; do not declare an active feed feature based on dormant code. |

Firebase/Google service-provider processing and intentional household sharing need
to be classified using the current form's definitions/exemptions; not every
transfer is automatically "sharing" for Play's form. Do not infer those answers
without reviewing contracts and final SDK behaviour. App/provider traffic uses
HTTPS, but local preferences/SQLite are not encrypted at rest by this change.

Publish policy/deletion URLs, verify that deletion works end to end, identify the
publisher consistently and declare any required retention. Support contact:
Callum@brightbark.co.uk. Final audience, content rating, ads and app-access forms
must match what reviewers can actually observe.
