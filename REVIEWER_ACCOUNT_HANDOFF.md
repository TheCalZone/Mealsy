# Reviewer accounts and email confirmation — 23 October 2026

## In-app creation using existing services

Account → Administrator → Create a new Premium account. The button appears after
a fresh server-owned administrator check. Enter an email inbox you control, a
password and grant reason/duration. Save credentials securely before confirming;
password copying is explicit, never automatic. Password fields are cleared after
attempting creation and on leaving. No password/token is written to preferences,
files, admin audit or diagnostic output. Clipboard contents should be cleared
using the device's clipboard controls after pasting into secure storage/Console.

Sign-up uses a separate HTTPS request with the app's public Firebase client API
configuration. It never replaces the current native Firebase login. A short-lived
new-user token is used only to request Firebase's verification email and is then
discarded. No new user receives admin claims. The existing setFamilyGrant callable
independently checks the acting administrator on the server before granting any
Premium. Creating an account does not create a subscription or take payment.

This flow can work with the existing deployed sign-up and complimentary-grant
services. It does not need the new email-confirmation admin action to be deployed.
API key/application restrictions, Firebase sign-up protection/password policy,
email delivery or quotas may still block it; test on a real device/project.
It does not bypass project security controls. Android package/certificate and iOS
bundle headers identify the configured application to supported API restrictions.

Creation and granting are separate operations. If the grant fails, the new account
may exist without Premium: use Retry grant or Find account by email. Never reset
an existing account's password automatically or silently give an existing account
a new grant. Ambiguous network failure requires lookup before another attempt.
A failed verification-email request does not hide successful account creation.
Use the inbox link (including from your phone) or sign into the new account on
another device and resend confirmation from Account. Do not give Play reviewers
your administrator credentials or ask them to complete verification themselves.
Keep the review account available and renew its complimentary grant as needed.

## Manual email-confirmed flag — backend deployment required

New administration action verifyEmail uses Firebase Admin SDK updateUser to set
emailVerified=true. lookupUser advertises canVerifyEmail; the app hides manual
confirmation against older backends. Current server Auth claims, not client flags
or an old token's admin claim, authorize each request. Disabled/deleting accounts,
missing/mismatched email and missing reasons are rejected. It does not change
passwords, Premium or admin claims. Only use it after independently confirming
ownership, such as your controlled reviewer inbox.

An audit intent is committed BEFORE calling Firebase Auth; the result is marked
completed or unconfirmed afterward. Auth and Firestore are not one transaction:
if the response or audit update fails, look up the actual Auth flag before claiming
success. Duplicate calls on an already verified account are no-ops. An email change
detected during the operation fails closed and leaves the returned address
unverified for review. Sign out/in on the reviewer device after confirmation to
refresh its sign-in token. No password or sign-in token belongs in the audit.

On the Firebase desktop workstation, merge this source with your local changes,
then run `node --test firebase/functions/test/*.test.js` and the normal shared
.NET tests. Stage using the existing codebase:

`firebase deploy --project STAGING_PROJECT_ID --only functions:households`

Test regular/disabled/revoked admins, non-admin spoofing, disabled/deleting targets,
changed emails, retries, Auth/API failures, audit outcomes and reviewer sign-in.
Only then deploy the reviewed codebase to the intended production project. No
production account, email, grant, verification flag or Firebase deployment was
performed by the cloud implementation/testing. Mock HTTP and backend tests are
not proof of project sign-up, mail delivery or deployed permissions.

## Navigation acceptance

Android active tabs use Material's checked state/indicator, bright icons and bold
labels. Unselected destinations remain labelled. Check all five tabs, nested
pages, theme changes, large text, rotation and shopping mode on a device. iOS uses
the shared contrast adjustment; the Android Material highlight is platform-specific.

Setup completion now clears the source tab's navigation stack before selecting
the destination. Reproduce Account → Get started → finish → Home → Account; it
must show Account rather than the completed guide. Test recipe/planner/shopping
finish shortcuts, skipping, sign-in during setup and explicit guide reopening from
Settings. Completing/skip hides Home/Account setup toolbar entries; an outstanding
invitation still has a dedicated Home entry. No other detail-page stacks are reset.
