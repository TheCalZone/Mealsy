#!/usr/bin/env bash
# Build a standalone APK using Android's existing development key.
# Keep AndroidSdkDirectory/JavaSdkDirectory selectors explicit when supplied.
set -euo pipefail
cd "$(dirname "$0")/.."
project=RecipeApp/RecipeApp.csproj
sdk_args=()
if [[ -n "${ANDROID_HOME:-}" ]]; then sdk_args+=("-p:AndroidSdkDirectory=$ANDROID_HOME"); fi
if [[ -n "${JAVA_HOME:-}" ]]; then sdk_args+=("-p:JavaSdkDirectory=$JAVA_HOME"); fi
# An incremental publish in the older Android SDK can retain native JNI method
# tokens from a previous linked Mono.Android.dll. That APK builds and signs but
# aborts before App is created. Always clean Release before generating test APKs.
dotnet clean "$project" -f net8.0-android -c Release "${sdk_args[@]}"
dotnet publish "$project" -f net8.0-android -c Release -m:2 \
    "${sdk_args[@]}" -p:AndroidPackageFormats=apk -p:AndroidKeyStore=false \
    -p:RunAOTCompilation=false -p:AndroidEnableProfiledAot=false
python3 scripts/check-android-startup.py \
    RecipeApp/bin/Release/net8.0-android/publish/com.companyname.recipeapp-Signed.apk
