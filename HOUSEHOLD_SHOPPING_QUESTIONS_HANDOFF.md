# Item questions, replies and household notifications

## App

Swipe a shared shopping item → Ask. The first question goes to the recorded adder/requester; for a legacy item (or one's own item), choose another household member. Subsequent replies notify the conversation's current household participants. Conversations are explicitly visible to the whole household, not private direct messages. Personal shopping items do not expose Ask.

Shopping options, Account and My household open Shopping questions & replies. The inbox supports manual refresh and browsing all household conversations. Adults/owners can clear a thread. Removed shopping items retain conversations so outstanding questions can still be read. The UI explains visibility and retains an unsent draft if sending fails. Conversation messages need an online connection.

Android: Phone alerts in the inbox options requests permission and registers this device. Alerts use generic text, without item/question content on the lock screen, and open the household inbox. Sign-out/disable still clears the local recipient binding. iOS shares the conversation/inbox UI; APNs/background alerts are not configured by this change and require local native work. Social feed/friends/business remain paused.

## Deploy on the configured Firebase project

Not deployed from this environment. Deploy the index in firebase/firestore.indexes.json and wait for it to finish building. Deploy these functions from the households codebase together:

- householdQuestions (new callable, us-central1)
- household (member leave/removal anonymization, plus earlier attribution work)
- notifications (allow household activity while social remains paused)
- deliverActivity (deliver household FCM data messages while social remains paused)
- processAccountDeletion (anonymize discussion authors/participants)

From the repository root, using the existing local Firebase workflow:

```
firebase deploy --only firestore:indexes
firebase deploy --only functions:households:householdQuestions,functions:households:household,functions:households:notifications,functions:households:deliverActivity,functions:households:processAccountDeletion
```

The existing callable deletion-marker guard wraps the new endpoint. Direct client access remains denied by existing rules. No public conversation collection or new client Firestore listener is introduced.

## Data, access and limits

One bounded document at households/{id}/conversations/shopping stores threads keyed by item GUID. The server checks current membership and owner entitlement in each transaction, takes the target from the trusted item's AddedBy and validates any legacy-item recipient against current membership. Clients cannot notify arbitrary accounts. Notifications use trusted household/item IDs, not user-supplied links. A conversation may be read/replied to after the item is removed; absent items cannot start new threads.

Each thread retains the latest 40 messages. At most 50 threads and 500 KB per household conversation document; adults can clear old threads via Browse household conversations. Each message is limited to 1,000 characters and each member to 60 sends per UTC day. Stable message IDs make immediate retries idempotent; deduplication lasts while the message remains in retained history. Recipients are the current member participants, excluding the sender. Notifications list the most recent 50 records, with a type/createdAt composite index and current-membership filtering. Existing 90-day notification deleteAfter/TTL setup still applies; verify it is enabled locally.

Leaving, removal and account deletion replace the contributor's message author ID with former-member, clear their participant/target references and remove their send quota record. Thread text authored by household members is retained for the household; it is not scanned for names or other personal text. Owned-household deletion removes the conversation tree through the existing recursive cleanup. The app privacy screen and firebase/desktop/privacy/index.html now explain this behaviour. Publish the updated public policy (including any separately hosted GitHub Pages copy) before production launch.

## Running cost

No feature is deployed or billed by making these source changes. There are no live chat listeners or automatic inbox refresh loops. Opening a thread reads membership, owner entitlement/grant, the bounded conversation document and the item document. Sending adds one conversation-document write plus one inbox write per other participant; FCM delivery runs only for new notifications and checks up to 10 registered devices. Device registration is refreshed at most daily during normal app resumes, or immediately when a token changes/enabling is requested. Inbox and conversation browsing read only when explicitly opened/refreshed. Reads for active shopping destinations are made only when that household target is selected, avoiding eager reads of all lists.

FCM has no per-message sending fee; Firestore operations, Functions execution, storage and applicable network charges still depend on the Firebase plan and region. Check real usage and budgets after staging; this is not a monetary cost guarantee.

## Staging checklist

Use two adults and a restricted member. Ask about an adult-added item, reply as the adder and verify requester attribution, unread inbox items and notifications on an opted-in Android phone. Repeat with an approved child request, a legacy item and an item removed after asking. Retry the same send; verify no duplicate message/alert. Test account switching/sign-out, leaving/removal, owner entitlement expiry, clear-thread controls and account deletion. Verify social feed routes and social notifications stay unavailable. Deny phone permissions and confirm the in-app inbox still works. Verify iOS inbox UI on a Mac/device; no native iOS test was possible here.
