#!/usr/bin/env bash
# Mac-only build entry point. Does not upload or install signing material.
set -euo pipefail
cd "$(dirname "$0")/.."
mode="${1:-simulator}"
case "$mode" in simulator|archive) ;; *) echo 'Usage: bash scripts/build-ios.sh [simulator|archive]' >&2; exit 2;; esac
if [[ "$(uname -s)" != Darwin ]]; then
    echo 'An iOS build requires macOS and a matching Xcode/.NET iOS workload.' >&2
    exit 2
fi
python3 scripts/check-ios-config.py
# Intentionally refuses an unsupported release baseline. Fix the migration;
# do not bypass this check and mistake a legacy archive for store readiness.
python3 scripts/ios-preflight.py
project=RecipeApp/RecipeApp.csproj
ios_target="$(python3 - <<'PY'
import xml.etree.ElementTree as ET
p=ET.parse('RecipeApp/RecipeApp.csproj').getroot()
targets={t for node in p.findall('./PropertyGroup/TargetFrameworks') for t in (node.text or '').split(';') if t.endswith('-ios')}
if len(targets)!=1: raise SystemExit('Expected exactly one iOS target framework.')
print(targets.pop())
PY
)"
args=("$project" -f "$ios_target" "-p:TargetFrameworks=$ios_target")
if [[ "$mode" == simulator ]]; then
    runtime=iossimulator-x64
    if [[ "$(uname -m)" == arm64 ]]; then runtime=iossimulator-arm64; fi
    dotnet build "${args[@]}" -c Debug -r "$runtime" -p:EnableCodeSigning=false
else
    : "${IOS_SIGNING_IDENTITY:?Set IOS_SIGNING_IDENTITY to your Apple Distribution certificate name}"
    : "${IOS_PROVISIONING_PROFILE:?Set IOS_PROVISIONING_PROFILE to your installed distribution profile name}"
    : "${IOS_BUILD_NUMBER:?Set IOS_BUILD_NUMBER to a new App Store Connect build number}"
    [[ "$IOS_BUILD_NUMBER" =~ ^[1-9][0-9]*$ ]] || { echo 'IOS_BUILD_NUMBER must be a positive integer.' >&2; exit 2; }
    dotnet publish "${args[@]}" -c Release -r ios-arm64 -p:ArchiveOnBuild=true -p:BuildIpa=true \
        "-p:CodesignKey=$IOS_SIGNING_IDENTITY" "-p:CodesignProvision=$IOS_PROVISIONING_PROFILE" \
        "-p:ApplicationVersion=$IOS_BUILD_NUMBER"
fi
