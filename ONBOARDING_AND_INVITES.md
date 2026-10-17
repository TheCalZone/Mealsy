# Invitations, getting started and complimentary Premium

## Sharing Mealsy

Home and Account toolbars, Settings and the final Get started step offer **Share
Mealsy**. This shares `https://mealsy-b732b.web.app/join/`, an Android download and
getting-started page. An account is free; a code is not required to sign up, and
onboarding never starts a subscription.

In **Account → My household → Invite a family member**, Share invitation includes
that landing link with the single-use household code in the URL fragment, plus a
plain copy of the code and instructions. Fragments are not sent in HTTP requests.
The existing household service creates, checks, expires and revokes the code; no
new kind of bearer code or Premium redemption code has been introduced. Existing
codes remain valid. The app accepts a raw code or an approved Mealsy invitation
link in both Get started and My household.

The page provides **Get Mealsy on Google Play**, **Copy household code** and
**Open Mealsy on Android**. An installed Android app handles `mealsy://join?code=…`
and `mealsy://welcome` through dedicated intent filters, separate from recipe-link
sharing. It saves a valid invitation on the device and opens Get started when the
app is ready. Joining always requires sign-in and explicit confirmation of account
email visibility; opening the link does not join automatically or enable Premium.

After a fresh install, the store does not automatically carry the code into the
app. Copy the code on the landing page first, then paste it in Get started or My
household after signing in. This does not use Firebase Dynamic Links, deferred
install attribution or verified Android HTTPS App Links. Custom scheme handling
and cold-start behaviour require phone testing. iOS URL opening is not implemented
by this update; the page explicitly says the iPhone release is not available.

### Activation

1. Pull/build this branch and distribute the updated app through your Play test or
   production track. The Play listing URL is a placeholder for your actual release
   availability: it cannot install an unpublished app or bypass closed-test access.
   Add friends/family to the test in Play Console and share the tester opt-in link
   before the listing link where required. No APK is hosted by this page.
2. Deploy the landing page alongside the existing Community Studio:

   ```sh
   npx --prefix firebase/functions firebase deploy --project mealsy-b732b --only hosting
   ```

   This updates the whole checked-in Hosting site, including the studio. No backend
   change is required; invitation validation and Premium grants use deployed APIs.
3. If the app package or Hosting domain changes, update `InvitationLinks.cs`, the
   join page's Play URL and browser script, and their tests together.

## Getting started

A new-device welcome card on Home links to a four-step optional guide: free versus
Premium tools, account/device mode, optional household joining, and first actions
for recipes, meals and shopping. It is always reopenable from Home, Account and
Settings. Skipping/completing hides the new-device card, but an unconsumed saved
invitation keeps an invitation reminder visible. Completed status is device-local.
New sign-ups open the guide; sign-in with a saved invitation opens the joining
step. Pending codes are retained through sign-in and failures and cleared after
successful joining. Basic solo use is available without joining a household.

## Complimentary Premium, with no purchase

Sign in to an administrator account, then choose **Account → Give complimentary
Premium**, or **Administrator → Find account / manage Premium**. Enter the
recipient's existing registered email. Choose 30, 90 or 365 days, or a custom 1–365
days; confirm the account/expiry and enter a reason. A new grant replaces the old
complimentary expiry from now, rather than automatically extending it. Revoke is
available on the same account card. Ask recipients to open Account → Premium or
Restore purchases to refresh their verified access.

This uses the existing `administration` action `setFamilyGrant`. The server checks
the caller's current Firebase Auth administrator claim on every request, validates
the target account and duration, and atomically writes `familyGrants` plus an
`adminAudit` event. No app-store charge, fake receipt, client Premium flag, promotion
credit or administrator role is created. Grant revocation does not cancel an
independent paid subscription; cached offline access may remain for up to one hour.
The grant covers personal Premium tools and, when held by the household owner,
Family access. Expiry retains recipes, lists and saved libraries.

If your own account does not yet have the admin role, a trusted project
administrator can bootstrap it from a Firebase-authenticated desktop:

```sh
node firebase/functions/set-admin.js mealsy-b732b YOUR_ACCOUNT_EMAIL grant
```

The app cannot grant its own administrator role. Do not give testers administrator
access just to enable their Premium features. Grant them complimentary access via
the account lookup instead. This change has not granted/revoked any live account.

## Phone checks

Share a generic link and a household invite to a second phone. Test installed and
fresh-install paths; code copy, sign-up/sign-in, skip, expired/revoked/used codes,
offline retry and wrong-account confirmation. Confirm personal lists remain
private and only explicit joining consumes the invitation. Use two test accounts
to verify grant, renewal, revocation, paid-subscription coexistence and expiry.
Normal users must never see a successful grant when calling the admin endpoint.
