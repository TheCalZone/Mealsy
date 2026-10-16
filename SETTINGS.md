# Appearance and device preferences

Open **Settings** from Home or Account, including without signing in. Changes save automatically on this device.

- **Colour theme:** Forest, Ocean, Berry, Terracotta or Slate. Navigation and interactive controls use the selected palette; surfaces and text use neutral colours for readability.
- **Display mode:** follow the phone, always light or always dark. Following the phone responds to system theme changes while Mealsy is running.
- **Larger reading text:** increases shared headings and reading text by 18%, in addition to normal system font scaling.
- **Recipe photos:** show or hide photos in recipe cards, reading screens and community cards. Photos remain stored and can still be edited.
- **Shopping defaults:** hide collected items and expand quick-add details when opening a list. The shopping screen's own controls can override these during a visit; opening an incoming recipe list reveals its new items.
- **Recipe order:** recent, name, quickest or top rated when opening the recipe book. The book still allows temporary sorting and filtering.
- **Cooking:** optionally start each recipe with screen-awake enabled. The recipe switch overrides this during cooking, and leaving releases the screen-awake state.
- **Reset preferences:** restores the defaults above, without deleting account data, recipes, lists, meal plans or drafts.

Preferences belong to the installation, rather than the current Firebase account. They do not sync to family members. Native Android widgets keep their own layout, system styling and selected shopping list. Themes do not change stored ingredient quantities or measurement units.

## Phone checks

The automated suite verifies safe loading of damaged/older settings, preservation of supported preferences and serialization. The Android build checks compiled XAML and platform code. Actual visual and lifecycle behaviour requires a phone:

1. In device mode, open Settings from Home, then repeat from Account. Choose each palette in both light and dark mode; check shopping, recipes, planner, household, community and forms for readable text and buttons.
2. Choose System, change the phone's dark mode while Mealsy is open, then restart Mealsy. Verify the saved palette and display choice persist.
3. Turn larger text on, including with a larger Android font size. Scroll the settings and recipe screens; check buttons and shopping fields remain usable.
4. Hide photos, open a recipe and the community, and show photos again. Confirm editor photos remain available and saved photos are unchanged.
5. Change both shopping defaults, leave Settings and open Shopping. Temporarily change Hide collected; it should work for this visit. Reopen the list to restore the chosen default.
6. Choose Quickest sorting, reopen Recipes and then try temporary sorting/search. Restart and verify the saved default applies.
7. Enable screen-awake, open a private recipe, preview and community recipe. Turn the recipe switch off/on and leave each screen; verify the normal phone timeout resumes.
8. Reset preferences, restart, and check the default appearance returned while recipes, shopping lists and meal plans remain.
