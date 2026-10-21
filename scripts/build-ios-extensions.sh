#!/usr/bin/env bash
# Build Swift app extensions; containing MAUI app is bundled separately.
set -euo pipefail
cd "$(dirname "$0")/../native/ios"
mode="${1:-simulator}"
configuration="${2:-Debug}"
[[ "$(uname -s)" == Darwin ]] || { echo 'Swift extensions require a Mac with Xcode.' >&2; exit 2; }
case "$mode" in simulator|device) ;; *) echo 'Use simulator or device.' >&2; exit 2;; esac
case "$configuration" in Debug|Release) ;; *) echo 'Use Debug or Release.' >&2; exit 2;; esac
command -v xcodegen >/dev/null || { echo 'Install XcodeGen on the Mac (brew install xcodegen).' >&2; exit 2; }
xcodegen generate --spec project.yml
sdk=iphonesimulator
destination='generic/platform=iOS Simulator'
if [[ "$mode" == device ]]; then sdk=iphoneos; destination='generic/platform=iOS'; fi
for target in MealsyShare MealsyWidget; do
    args=(-project MealsyExtensions.xcodeproj -scheme "$target" -configuration "$configuration"
          -sdk "$sdk" -destination "$destination" -derivedDataPath build
          "MARKETING_VERSION=${IOS_APP_VERSION:-1.0}" "CURRENT_PROJECT_VERSION=${IOS_BUILD_NUMBER:-1}")
    if [[ "$mode" == simulator ]]; then
        args+=(CODE_SIGNING_ALLOWED=NO)
    else
        : "${IOS_TEAM_ID:?Set your Apple development team ID}"
        : "${IOS_SIGNING_IDENTITY:?Set your installed signing identity}"
        if [[ "$target" == MealsyShare ]]; then
            : "${IOS_SHARE_PROFILE:?Set the installed share-extension provisioning profile name}"
            profile="$IOS_SHARE_PROFILE"
        else
            : "${IOS_WIDGET_PROFILE:?Set the installed widget provisioning profile name}"
            profile="$IOS_WIDGET_PROFILE"
        fi
        args+=(CODE_SIGN_STYLE=Manual "DEVELOPMENT_TEAM=$IOS_TEAM_ID" "CODE_SIGN_IDENTITY=$IOS_SIGNING_IDENTITY" "PROVISIONING_PROFILE_SPECIFIER=$profile")
    fi
    xcodebuild "${args[@]}" build
    [[ -d "build/Build/Products/$configuration-$sdk/$target.appex" ]] || { echo "Missing $target.appex" >&2; exit 2; }
done
