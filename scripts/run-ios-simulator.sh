#!/usr/bin/env bash
# Explicit Mac action: build, boot/select Simulator, install and launch Mealsy.
set -euo pipefail
cd "$(dirname "$0")/.."
[[ "$(uname -s)" == Darwin ]] || { echo 'An iOS Simulator requires a Mac with Xcode.' >&2; exit 2; }
export IOS_INCLUDE_EXTENSIONS="${IOS_INCLUDE_EXTENSIONS:-0}"
bash scripts/build-ios.sh simulator
inventory="$(mktemp)"
trap 'rm -f "$inventory"' EXIT
xcrun simctl list devices available --json > "$inventory"
selection=("$inventory")
if [[ -n "${IOS_SIMULATOR_UDID:-}" ]]; then selection+=(--udid "$IOS_SIMULATOR_UDID"); fi
simulator="$(python3 scripts/select-ios-simulator.py "${selection[@]}")"
state="$(python3 - "$inventory" "$simulator" <<'PY'
import json,sys
with open(sys.argv[1]) as f: devices=json.load(f)['devices']
print(next(d['state'] for group in devices.values() for d in group if d['udid']==sys.argv[2]))
PY
)"
if [[ "$state" != Booted ]]; then xcrun simctl boot "$simulator"; fi
xcrun simctl bootstatus "$simulator" -b
open -a Simulator --args -CurrentDeviceUDID "$simulator"
runtime=iossimulator-x64
if [[ "$(uname -m)" == arm64 ]]; then runtime=iossimulator-arm64; fi
app="RecipeApp/bin/Debug/net10.0-ios/$runtime/RecipeApp.app"
[[ -d "$app" ]] || { echo "Build succeeded but expected app was not found: $app" >&2; exit 2; }
xcrun simctl install "$simulator" "$app"
xcrun simctl launch --terminate-running-process "$simulator" com.companyname.recipeapp
echo 'Mealsy launched. Verify its screen and logs; this command does not establish runtime acceptance.'
