# UI flow review — 12 November 2026

The main task stays visible; secondary controls open on demand. No backend, pricing, entitlement or permission rules changed. Social remains paused.

| Area | Changes / source review |
| --- | --- |
| Home | Shared animated drawers; removed redundant tab shortcuts; one concise Premium route. |
| Account | Grouped profile/sync, household, Premium, privacy/deletion and conditional admin/community tools. Sign out remains visible. |
| Settings | Grouped appearance, shopping, cooking and help/account; removed inactive preview action. |
| Recipe book | Search and create/import remain visible; filters/sort/reset in one drawer. |
| Recipe creation/edit | Optional photo, metadata and paste tools collapse; ingredient/step actions use compact menus; footer Save/Preview retained. |
| Recipe reading | Ingredients and method remain visible; servings/keep-awake grouped. |
| Personal meals | Week settings collapse; day actions grouped; shopping review fixed at bottom. Day editor has Save/Cancel footer. |
| Household meals/members | Shared disclosure pattern; meal actions grouped; requests, roles and destructive confirmations retained. |
| Shopping | Existing compact list and floating adder retained; List tools grouped; replacement action belongs in creator options and keeps destination confirmation. |
| Authentication | Required fields first; optional registration details collapse; submit/status in footer, loading overlay retained. |
| Navigation | Options reflect currently available/enabled actions; completed setup shortcut filtered; shopping navigation uses common guard. |
| Premium/privacy/admin | Existing plan disclosures, restore/cancellation information, privacy/deletion and permission checks retained. No live purchases/admin actions performed. |

## Validation

- Android `net10.0-android`, `android-x64`, Release APK build: passed, zero errors; existing compiler/XAML warnings remain.
- Shared tests: 451 passed, zero failed.
- Whitespace check: passed.
- Native UI verification attempted with a fresh API 34 software emulator; its Android package service failed during installation. No new screen is claimed visually verified.
- iOS changes share MAUI source; no iOS build or simulator test is possible on this Linux host.

## Phone acceptance before distributing

1. On a small screen, expand/collapse drawers in light/dark themes and with large text and reduced motion; verify content reflows, scrolls fully and shows focus/readable labels.
2. In guest mode, test recipe filters/reset, recipe editing/pasting, ingredient and step menus, reading/servings, meal editing/copy/clear and shopping review/destination selection.
3. Open sign-in and registration with the keyboard visible; check submit, loading, validation and Back. Finish setup and revisit Home/Account options.
4. With test accounts, check confirmed/unconfirmed email, role-specific household/admin tools, member drawers, shared meals and requests. Privacy/deletion and Premium/restore routes must remain reachable; do not delete production accounts to test navigation.
5. Validate the same routes on iPhone with Xcode, especially disclosure layout, keyboard avoidance, action sheets and safe-area footers.

The x64 build is an emulator artifact, not a phone APK. Version code 8 is source preparation, not a Play submission.
