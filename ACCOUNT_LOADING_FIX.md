# Account refresh and loading feedback

The Account tab embeds its sign-in form. Navigating to `//AccountPage` after
sign-in when that tab is already selected does not trigger `OnAppearing`. The
old form therefore stayed visible until the user changed tabs.

The form now calls the owning page directly after authentication. The page
immediately renders signed-in/cached details, then refreshes profile, community
name and administrator status in parallel with an animated indicator. Request
versions and captured account IDs prevent results from an older refresh, another
account or a page that has been left from updating the visible account. Username
editing waits for hydration so the final render cannot discard a newly typed name.
Failed secondary loads keep the signed-in view and show a retry action.

Sign-in, registration and password reset have a native animated progress overlay
outside the disabled form, with operation-specific messages and cleanup on failure.
Home, recipe book, recipe details and planner initial loads now use animated
loading rows. The community feed shows progress for initial interest checks,
refresh and pagination, and recipe editing indicates busy operations. Shared
buttons display a working hourglass/caption only when an action exceeds 300 ms;
short navigation avoids flicker, and task-owned updated labels are preserved.

No authentication protocol, Firebase rules, account permissions, pricing or backend
code changes are needed. This fixes the Android branch; the separate iOS branch
can merge/cherry-pick the shared client fix when ready.

Phone checks: sign in directly inside Account without switching tabs; verify the
form disappears immediately. Repeat from the separate login screen and after
sign-out; test incorrect credentials, password reset, slow/offline connection,
retry, rapid tab changes and switching between two accounts. Confirm old profile
or administrator controls never appear for the new account. Check feed loading
stops after success/failure and recipe save/photo work retains drafts on failure.
Automated request-version tests cover superseded, signed-out, changed-account and
page-exit responses; native animation and UI timing still require phone testing.
