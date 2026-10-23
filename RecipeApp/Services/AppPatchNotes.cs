namespace RecipeApp.Services;

public record AppPatchNote(DateTime Date, string Title, IReadOnlyList<string> Changes);

/// <summary>Bundled, newest-first release history. Add an entry when shipping app changes.</summary>
public static class AppPatchNotes
{
    public static IReadOnlyList<AppPatchNote> Entries { get; } = new AppPatchNote[]
    {
        new(new(2026,10,23), "Choose your list and ask before you buy", new[] {
            "Recipe ingredients, meal-plan shops, Home quick-add, saved shopping trips and device-list copies ask which list to use. Household lists appear alongside named personal lists; cancelling changes nothing. Replacing a list names the chosen personal list before confirmation.",
            "Swipe a household item and choose Ask to clarify details with its adder. Conversations are visible within the household; questions and replies have an inbox, with optional Android phone alerts. This requires the new Firebase questions, notifications, cleanup and index deployment.",
            "No live conversation listeners or background inbox polling. Messages, history and device registration are bounded to keep household costs down."
        }),
        new(new(2026,10,23), "Who added it?", new[] {
            "New shared shopping items show a small member-coloured Added by badge. Approved requests show Requested by and keep the original requester’s name.",
            "Attribution requires the updated Firebase household function. Older items remain unlabelled, and personal lists stay uncluttered."
        }),
        new(new(2026,10,23), "Widget controls and themes", new[] {
            "The middle widget icon opens a dedicated list chooser, with item-entry controls hidden. Saving returns you to the app screen beneath it.",
            "Widgets and the home-screen add dialog follow your selected colour palette and light/dark appearance. Changing the app theme refreshes installed widgets."
        }),
        new(new(2026,10,23), "Name your household list", new[] {
            "Household owners can rename their shared list from Account → Household → Household membership. The name also identifies shared meal plans; items and members are preserved.",
            "Renaming requires the updated household backend. Shared shopping refreshes its name when reopened."
        }),
        new(new(2026,10,23), "More room for your shopping list", new[] {
            "Shopping now opens with a compact list name, sharing label and remaining count. Lists and options live in a quick animated drawer; the item adder has its own drawer.",
            "Drawers are height-limited and only one opens at a time, keeping the list visible. Undo stays within reach; extra actions move out of the main list area."
        }),
        new(new(2026,10,23), "Smoother home sections", new[] {
            "Home drawers now slide open and closed quickly, moving the content below them instead of jumping. System reduced-motion settings are respected."
        }),
        new(new(2026,10,23), "Clearer navigation and reviewer setup", new[] {
            "The selected Android bottom tab now has a visible highlight, bright icon and bold label. Other tabs stay labelled without shifting.",
            "Finishing setup removes the guide from its original tab, so returning to Account opens your account. Completed setup shortcuts disappear from Home and Account; the guide remains available in Settings.",
            "Administrators can create a separate reviewer account without signing themselves out, send its verification email and grant complimentary Premium. Manual email confirmation is available after the administration backend update."
        }),
        new(new(2026,10,23), "Household roles and approval requests", new[] {
            "Owners can assign Adult or Restricted access and choose roles for invitations. Existing members keep Adult access until changed.",
            "Restricted members can check shopping items, but new shopping items and meals wait for owner/adult approval. Personal lists remain unrestricted. Requests can be approved, declined or cancelled from Household requests.",
            "These permissions require the updated Firebase household and meal functions. Backend rollout and staging tests must finish before relying on restricted access."
        }),
        new(new(2026,10,23), "Clearer household management", new[] {
            "Family members have expandable cards showing their role and available actions. Shared shopping and meal plans have clear shortcuts.",
            "Invitations, membership, device-list import and permanent household deletion are grouped into separate drawers. Only owners see invitation and removal controls."
        }),
        new(new(2026,10,23), "Your lists, clearly separated", new[] {
            "Switch between named lists using the scrolling tabs. Active Premium households open their shared list by default; explicit widget and recipe destinations take priority.",
            "Free includes two personal lists; Premium lets you create more. Additional lists are stored on this device. Existing lists remain editable when Premium ends.",
            "Rename or delete additional lists from list options, choose a personal destination for recipe imports, and configure each Android widget for a named list. Quick-add and visibility labels show where items go."
        }),
        new(new(2026,10,22), "Choose what happens to checked shopping items", new[] {
            "Shopping's Checked item settings can keep checks visible, hide them, or delete items as you check them. Automatic removal supports Undo while the list stays open.",
            "The setting stays on this device. Deleting from a household list affects everyone; existing checked items and remotely checked items are not automatically removed."
        }),
        new(new(2026,10,22), "Account navigation icon", new[] {
            "The Account tab now has a clear person icon alongside its label, matching the other navigation tabs."
        }),
        new(new(2026,10,22), "Discover household sharing", new[] {
            "Home shopping and meal sections now offer household shortcuts with relevant Premium benefits. Covered members go straight to their shared list or plan without needing a separate subscription.",
            "Inactive household members are directed to their owner's plan, while Premium users without a household can set one up."
        }),
        new(new(2026,10,22), "A calmer home page", new[] {
            "Home sections now expand with a tap, keeping shopping, today's meals, favourites and kitchen tools easy to scan. Main shortcuts stay visible.",
            "Removed household check timestamps and routine account-status text. Sections keep their open state while you use the page, and reset when accounts change."
        }),
        new(new(2026,10,22), "Public privacy and deletion instructions", new[] {
            "Public policy pages are prepared for GitHub Pages, and the app uses the corresponding privacy and deletion-instructions URLs. Hosting needs the repository Pages setting enabled.",
            "The public deletion page supports email requests without app installation and explains when the automated in-app service is available."
        }),
        new(new(2026,10,22), "Privacy and release-readiness review", new[] {
            "Settings and Account now offer privacy/data information, automated Firebase account-deletion requests with recent password confirmation, and an email fallback. Public privacy and deletion pages are supplied for publisher review and deployment.",
            "Optional diagnostics default to off when no choice was saved; Android private-cache backup and advertising-ID permission are disabled in this source. Existing explicit diagnostics choices remain.",
            "This is a test release. Automated deletion needs the new Firebase functions, rules and indexes deployed and verified. The audit also identifies target SDK/native compatibility, published policies and store setup as release blockers."
        }),
        new(new(2026,10,22), "Eligible seven-day Premium trials", new[] {
            "Google Play subscription options can now show an eligible seven-day trial, its renewal price and cancellation terms. Users without an eligible trial see the standard subscription price.",
            "Checkout uses the selected store offer and stops if its terms change. Payment details stay with Google Play. Trial availability requires Play Console configuration and an eligible Play account.",
            "Premium promotions now invite you to check trial eligibility. iOS purchases and trials remain unavailable in this build."
        }),
        new(new(2026,10,22), "Helpful account and Premium invitations", new[] {
            "Shopping and meal-plan screens now offer contextual sign-up or Premium benefits prompts. Creating an account is free; household sharing needs Premium.",
            "Prompts explain saved shopping trips, reusable meal weeks and shared household plans without claiming unlimited cloud lists. Active Premium users do not see them, and Not now hides contextual prompts for seven days.",
            "Signed-out users trying Premium tools or shared shopping get a direct Create free account action. Existing sign-in options remain available. Prompts make no extra cloud requests."
        }),
        new(new(2026,10,22), "Discover Mealsy Premium", new[] {
            "Home and Account now show a Premium benefits card for free users, highlighting recipe-link importing, reusable weeks, saved shopping trips and household sharing.",
            "The card opens the existing features and subscription pages without making a purchase. It hides for active Premium members, including covered family members, and Not now snoozes it for seven days. No extra cloud requests are made."
        }),
        new(new(2026,10,21), "iPhone widget and recipe sharing", new[] {
            "Native iOS extension source now supplies a medium quick-shopping widget. Add opens Mealsy's quick-add screen; checklist and sliders open the selected list and list selection.",
            "Share to Mealsy accepts browser recipe links into a local inbox. Open Mealsy to review or discard a link before using the existing importer. Apple App Group/signing setup and native Mac verification are required."
        }),
        new(new(2026,10,21), "iOS client handoff refreshed", new[] {
            "The iOS client branch now includes the latest shared shopping, recipe, meal-plan and household loading improvements.",
            "iOS section dragging uses compact-section callbacks and checks that its account and list remain unchanged. Native device verification and release setup remain required."
        }),
        new(new(2026,10,21), "Keep your place while planning a shop", new[] {
            "Switching between Snacks and Essentials updates the checklist in place instead of rebuilding the screen and jumping to the top.",
            "Cupboard-check and review screens remember their scroll positions when moving between steps or returning from an item editor."
        }),
        new(new(2026,10,21), "Faster household overview with fewer repeat reads", new[] {
            "Household shopping and the current shared meal week preload before the existing account sync. Home shows today's household meals, items left to buy, saved-view timestamps and direct shortcuts.",
            "Screens share recent read results and simultaneous requests. Unchanged shopping checks reuse results for a minute instead of contacting the server every five seconds; edits and manual refresh still reach the backend.",
            "Shopping polling pauses in the background and backs off for a minute after connection failures. Household preload is read-only and skips history and separate photo downloads.",
            "Cached snapshots are account/household specific and displayed only after a recent membership check. Writes always use server permission checks; widget additions retain their fresh access check. Existing offline edits and account syncing are kept."
        }),
        new(new(2026,10,20), "Share recipe pages straight to Mealsy", new[] {
            "Android's Share menu now has a Save recipe to Mealsy target for browser page links. It opens the recipe importer with the shared link filled in.",
            "Handles shared titles, formatted text and supported links among multiple URLs. Importing waits for startup and stays behind your Import & review action.",
            "Recipe-link importing still requires Premium and a supported recipe website. Existing drafts are kept and photos remain optional."
        }),
        new(new(2026,10,20), "Move shopping sections as compact bundles", new[] {
            "When you start dragging a section, ingredient rows temporarily fold away so they no longer obstruct the section cards or drop gaps. Each section keeps its item count.",
            "Drop the section to expand its ingredients in the new position. Cancelling restores the expanded list without changing its order.",
            "Move icons, floating previews and continuous edge scrolling remain available. Ingredient names, quantities, collected checks, filters and item order are kept."
        }),
        new(new(2026,10,20), "Continuous scrolling while dragging", new[] {
            "Hold an item or section near the top or bottom edge to scroll steadily, without needing to keep wiggling your finger. Scrolling speeds up gently closer to the edge.",
            "The floating card and insertion gap keep updating as rows scroll into view, even after the original row has left the screen.",
            "Moving back toward the middle, releasing, cancelling or leaving the app stops automatic scrolling. Dragging still starts only from the move icons."
        }),
        new(new(2026,10,20), "Move sections using the handle only", new[] {
            "Section dragging now starts only from the dedicated move icon, both in the shopping list and the section-order screen. Item dragging also remains handle-only.",
            "Touching section names, counts or the rest of a section card scrolls normally. Floating previews and drop gaps remain available when using the handle."
        }),
        new(new(2026,10,20), "Clearer animated shopping reordering", new[] {
            "Shopping items now lift into a floating preview that follows your finger, while neighbouring rows slide aside and a full-size Drop here gap shows the insertion point.",
            "The same Android drag preview now works for shopping section headers and the Reorder sections screen, without a long-press delay. Buttons remain available as an alternative on the section-order screen.",
            "Light haptic feedback marks pickup and destination changes. Releasing commits the move; cancellation restores the original layout."
        }),
        new(new(2026,10,20), "Easier shopping rows and direct dragging", new[] {
            "Item names, quantities and notes now sit together on the left; the right side is reserved for the move handle.",
            "On Android, touch the handle and move immediately without a long-press delay. A highlighted row shows where release will place the item, with edge scrolling while you move.",
            "Ordering stays within the item's section and is saved for the selected list on this device. Checkboxes, item taps and swipes keep their existing actions."
        }),
        new(new(2026,10,20), "Use what you have", new[] {
            "Replaced the AI recipe creator with a free offline finder for your saved recipes, available from Home and the recipe book.",
            "Enter leftover ingredients in everyday wording. Minced beef, beef mince and ground beef match the same ingredient. Multiple ingredients rank by how many match, with clear explanations.",
            "Open a matching recipe or review its shopping ingredients. This searches saved ingredient lists; it does not generate recipes, infer quantities or assume you have every other ingredient."
        }),
        new(new(2026,10,20), "Homepage recipe shortcuts", new[] {
            "Tap recipe cards to open the recipe directly. Hold a homepage recipe card on Android for View, Edit, Shop ingredients and Delete shortcuts.",
            "A visible Options button provides the same menu. Delete asks for confirmation, checks account ownership and removes the cloud copy before the local recipe."
        }),
        new(new(2026,10,20), "Shopping suggestions for typed meals", new[] {
            "Weekly shopping review works even when no saved recipes are selected.",
            "Typed meals without recipe attachments suggest matching saved essentials, with meal names and weekly occurrence counts. Nothing is selected automatically.",
            "Choose your usual purchase once for the week, adjust amounts in review, or save a missing essential for next time. Meal names do not generate guessed ingredient lists."
        }),
        new(new(2026,10,20), "Compact home-screen quick add", new[] {
            "The widget Add icon opens a small native entry dialog over the home screen, without navigating through Mealsy. Android widgets cannot host an editable text field directly.",
            "Replaced large widget buttons with accessible Add, Choose list and Open list icons. Each widget keeps its own personal or household selection.",
            "Quick add recognises quantities such as 500g minced beef, keeps offline personal saves and checks household access before adding."
        }),
        new(new(2026,10,19), "Language support", new[] {
            "Added automatic device-language selection for English, Spanish, French, German and Portuguese, with English fallback.",
            "Choose a language in Settings and restart Mealsy to apply it. Recipes, lists and saved identifiers are kept unchanged.",
            "Translated core navigation, account forms, shopping controls, recipe editing and meal-planning controls. Regional dates, numbers and Play prices follow the device/store settings.",
            "Advanced/admin text, some help and backend errors still fall back to English. Recipe content and ingredient names are not automatically translated."
        }),
        new(new(2026,10,19), "Consistency improvements for testers", new[] {
            "Account and household shortcuts no longer show fixed subscription prices. Monthly and annual prices on the upgrade screen come from Google Play, with a clear unavailable state and refresh action.",
            "Premium descriptions now include shared household meal planning and explain which personal data stays on this device.",
            "Typed meals appear on the home dashboard. Recipe-link importing is also available directly from the recipe book.",
            "Improved household sign-in links, button styling and navigation between Premium plans and features. Social features remain paused."
        }),
        new(new(2026,10,19), "Administrator patch notes", new[] {
            "Added this read-only change history to the administrator panel.",
            "The administrator screen keeps its own title and opens without the email keyboard covering the tools. The complimentary Premium shortcut still focuses the email field."
        }),
        new(new(2026,10,19), "Startup crash fix & Mealsy splash title", new[] {
            "Fixed the test APK packaging mismatch that caused the app to crash immediately after the splash screen.",
            "Added Mealsy beneath the food illustration on the themed splash screen.",
            "Added clean packaging and startup checks to catch the same APK mismatch before sharing future test builds."
        }),
        new(new(2026,10,19), "Easier typing & recipe importing", new[] {
            "Android screens resize above the keyboard. Recipe Preview and Save remain in a fixed footer while editing.",
            "Recipe-link importing includes a Save recipe photo, if available checkbox. Missing or blocked photos do not stop the recipe from importing.",
            "Import a recipe link directly into a personal meal slot or a household meal. Review and save the recipe, then save the meal plan.",
            "Household recipe attachments ask before sharing a snapshot. Recipe-link importing remains a Premium feature."
        }),
        new(new(2026,10,19), "Cooking checklists & clearer options", new[] {
            "Recipe steps suggest ingredient checkboxes, with manual ingredient selection when needed. Each step has its own temporary checklist.",
            "Ingredient amounts follow the selected servings and show recipe totals; follow the method for divided quantities.",
            "Page toolbar actions are grouped under a labelled Options menu.",
            "Android startup and top-bar colours now match Mealsy's green theme."
        }),
        new(new(2026,10,18), "Shared household meals", new[] {
            "Connected household meal planning to its Firebase backend, including shared recipe snapshots and conflict handling.",
            "Enforced the social-feature pause on the server and integrated the iOS preparation work. iOS still requires its own Apple build and device testing."
        }),
        new(new(2026,10,17), "Faster, more flexible shopping lists", new[] {
            "Added offline ingredient suggestions while typing, including the Android quick-add widget.",
            "Added drag handles for shopping items and long-press dragging for sections. Layout order is saved for each list on your device.",
            "Recognise quantities in names such as 500g minced beef; an explicitly entered quantity takes priority.",
            "Improved aisle recognition for meat, herbs, pantry items, frozen foods and common household products.",
            "Paused social, friends and creator screens while keeping household shopping and meal planning available."
        })
    };
}
