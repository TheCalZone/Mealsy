# Android widget checks

The + button opens the native quick-add dialog above the launcher; the middle sliders button opens a dedicated list chooser; the last button opens the saved list. The chooser hides item entry and saves one target per widget. Choosing a different target does not change other widgets. For household targets, use Find my household list, then Remember. Existing account/access checks remain enforced.

Widget colours and the native dialog use the same palette resolver as the app. Explicit Light/Dark settings override the system; System follows Android. Saving settings refreshes existing widgets; Android application configuration changes refresh them too. Rounded light/dark widget backgrounds and icon/text colours are applied through RemoteViews (not MAUI resources).

On a phone: keep two widgets with different targets; exercise all three icons on cold and warm app starts; change each widget's target, reopen the chooser and confirm persistence; change palette and appearance from Settings and check both widgets plus the + dialog; change Android dark mode with System selected; repeat with a household and after switching accounts. No physical launcher/emulator validation was available in this environment.
