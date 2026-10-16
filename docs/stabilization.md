# Stabilization review

## Changes in this pass

- Serialize SQLite initialization and let initialization failures reach callers.
- Correct SQLiteNetExtensions foreign-key annotations.
- Save recipe and ingredient edits in a transaction; clean up ingredients when recipes are deleted.
- Keep local snack and essential caches current after insert/delete.
- Repair incomplete default meal plans in place instead of creating another plan.
- Restore the meal editor's selected recipes, clear all three primary IDs consistently, and serialize empty extras/notes.
- Load extra meal recipes and show extras when either recipes or notes exist.
- Use complete filtered-source collections when generating shopping lists; honor list-checker answers and allow missing tags.
- Accept meal-planner JSON, avoid double URL decoding, and avoid duplicating lists on page reappearance.
- Persist generated list edits/checkmarks, repair undo/redo, and preserve section headings and notes when parsing manual lists.
- Save imported/uploaded recipe images and cloud recipe tags/images; retain snack/essential ownership and timestamps.
- Await startup/login sync and contain several asynchronous failure paths.
- Align MAUI package versions with .NET 8, remove Windows COM references and duplicate resources, and remove duplicate Android editor command wiring.
- Remove embedded OpenAI keys, Android signing passwords, the committed keystore, and service account credential file from the proposed branch.

## Required verification (not yet run)

1. Build the iOS simulator target on a Mac with compatible .NET 8 MAUI and Xcode.
2. Fresh install, offline launch, and relaunch with existing local data: all pages load without missing-table errors.
3. Create, edit, delete, and reopen recipes with/without images and with multiple ingredient sections. Check edits persist and ingredient rows do not duplicate.
4. Add/delete/edit snacks and essentials; verify cache, filters, and local persistence.
5. Select recipes/snacks/essentials, apply a search/filter, then generate a list. Selected items hidden by the filter must still be included.
6. Enable list checker: answer Yes/No to different items. Only No items should appear. Include a null-tag item.
7. Open each meal day, select all three meals, add extra recipes and notes, save, reopen, and save without changing selections. Clear selections and verify cleared values remain cleared.
8. Generate a list from a meal plan. Verify extra recipes are included and repeated generation does not accumulate counts.
9. Edit a generated list, tick items, leave/reopen and relaunch; verify persisted edits. Confirm an alert does not duplicate/reset the list.
10. Manual list: parse LF and CRLF text, section headings, complete and incomplete parentheses, blank input, and names containing plus signs.
11. Undo/redo item and group additions/removals, including removal of the last item in a group. Check item counts, restored group placement, and redo invalidation after a new edit.
12. Sign in/out, restart, and test cloud outage recovery. Verify recipe tags/images and snack/essential metadata round-trip in a test Firebase project.
13. AI page: logged out, missing key, request failure, malformed response, and repeated saves must not crash or leave the spinner running.

## Remaining issues before a dependable baseline

- Cloud documents use SQLite numeric IDs as identity. Different devices can generate the same IDs. A stable cloud ID plus a migration of existing documents/references is still needed; do not assume multi-device synchronization is reliable.
- Cloud deletions have no tombstones, and sync algorithms are duplicated across startup, account, and home pages. Offline deletions can be resurrected. Consolidate sync and test conflict/deletion behavior before relying on it.
- Account data shares one SQLite database; signed-out views and some meal-planning paths are not fully isolated by account. A complete account-switch/data ownership policy needs implementation and integration tests.
- Some page lifecycle/command paths still use async void; a full error-handling pass remains.
- Recipe URL import is an existing placeholder that returns no recipe. AI key setup has no UI/backend integration.
- Manual list text persistence preserves names/notes/sections but not basket state. Generated lists persist basket state as JSON.
- No automated test project or CI currently verifies these paths.
- .NET 8 / MAUI 8 and the old Firebase/plugin packages need a separately validated upgrade for supported SDKs/Xcode. Android, Windows, and MacCatalyst are not validated targets in the current project.

## Exposed credentials

The public repository contains an OpenAI key, Firebase service-account credential file, and an Android signing keystore/passwords. Treat them as exposed even after deletion in this branch: revoke/rotate the OpenAI and service-account credentials, audit usage and Firebase rules, and follow the Android signing provider's key-reset procedure if applicable. History and existing downloads retain old copies. No credentials were used for live API or database access during this review.

Firebase mobile configuration files identify the client project and are retained; they do not replace server-side access rules.
