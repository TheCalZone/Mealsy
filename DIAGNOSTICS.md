# Error reporting

Handled operational failures now generate bounded, sanitized non-fatal Crashlytics
events through the existing platform SDK. Covered paths include callable requests
(feed, shopping/household, billing, administration), account sign-in/registration,
profile loading and sync, common action handlers, and uncaught managed exceptions.
Existing native fatal collection remains enabled by default. Managed exception
hooks do not mark exceptions handled or prevent termination.

Report fields are fixed operation names, error categories, failure reasons and a
random reference. Sanitized managed frames contain application method names only:
no paths, source lines, raw exception messages, inner exceptions, request bodies,
response bodies, photos, recipe text, email addresses or account identifiers are
attached by this reporter. Native SDK reports include normal app/device metadata;
this is not a promise to remove everything independently captured by a native SDK.

One event per operation/category/reason per minute and at most 20 per process
session are submitted. Twenty recent references are kept in device preferences.
Repeated exceptions aren't reported twice. Failures in diagnostics never interrupt
the app's save/recovery flow. Local records distinguish queued, grouped and local
only reports; queued means accepted by the SDK, not confirmed delivered.

Settings > Diagnostics & error reports offers collection opt-out, recent/copyable
references and a safe synthetic test. Disabling reporting stops new custom event
submission, disables native automatic collection and discards pending SDK reports.
Already delivered events remain in the console. Local reference history remains.
The publisher's privacy disclosure should describe diagnostic collection before
public distribution.

## Server correlation and moderation

Callable failures include `details.reference` and `details.reason`. A wrapper logs
operational failures as structured errors with that reference, fixed service and
action names, category and safe error type. No request body, identity, raw provider
error, or response is logged by the wrapper. Look up a supplied reference in cloud
function logs; matching client events include the same `report_reference` key.

Safety reasons distinguish SAFETY_REJECTED, SAFETY_BUSY, SAFETY_TIMEOUT,
SAFETY_UNAVAILABLE and SAFETY_RESPONSE_INVALID. Checks still fail closed. Content
rejection is an informational server event, not a crash-reporting bug. Validation,
incorrect credentials and normal permission denials are not sent as non-fatal
issues. User-facing wording remains provider-neutral and preserves pending edits.

## Verification

Run `dotnet test RecipeApp.Tests/RecipeApp.Tests.csproj` from a compatible SDK setup,
and `node --test firebase/functions/test/*.test.js` for server tests. Coverage
includes actual report adapter inputs/redaction, opt-out, duplicate suppression,
SDK failure recovery, fixed metadata allowlists, shared references, and simulated
busy/timed-out/malformed moderation responses.

To verify delivery, install the diagnostics test APK on a phone, enable reporting,
send one test diagnostic, then close/relaunch normally and inspect the project's
Crashlytics dashboard. SDK uploads may be delayed; avoid treating an immediate
absence as proof of failure. This workspace's build/tests do not prove a real
phone report reached the console. iOS reporting still needs a Mac/device build
and its symbol-upload setup checked before release.
