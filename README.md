# Mealsy Android widget fixes — da19be1

The middle sliders icon now opens dedicated widget list settings rather than item entry. Widget surfaces, text, icons and native quick-add dialog follow the selected app palette and appearance. Settings changes refresh existing widgets.

Source: codex/android-runtime-fixes at da19be1. 437 shared tests passed. Android build and clean Release publish checked. Existing development signer, archive and JNI startup checks verified. No physical launcher testing here; see WIDGET_TESTING.md in source for device checks. No new Firebase deployment is needed for these widget fixes.

This is a development-signed test APK, not a Play production release. Existing target SDK and native 16 KB compatibility limitations remain.
