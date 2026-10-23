#!/usr/bin/env python3
"""Check public iOS bundle/config metadata without outputting credentials."""
from pathlib import Path
import plistlib
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
project = ET.parse(root / 'RecipeApp/RecipeApp.csproj').getroot()
bundle = project.findtext('./PropertyGroup/ApplicationId')
config = plistlib.loads((root / 'RecipeApp/Platforms/iOS/GoogleService-Info.plist').read_bytes())
info = plistlib.loads((root / 'RecipeApp/Platforms/iOS/Info.plist').read_bytes())
privacy = plistlib.loads((root / 'RecipeApp/Platforms/iOS/PrivacyInfo.xcprivacy').read_bytes())
errors = []
if config.get('BUNDLE_ID') != bundle:
    errors.append('Firebase iOS bundle ID does not match ApplicationId.')
if config.get('PROJECT_ID') != 'mealsy-b732b':
    errors.append('Firebase iOS project does not match the current callable endpoints.')
if not config.get('GOOGLE_APP_ID', '').startswith('1:') or ':ios:' not in config['GOOGLE_APP_ID']:
    errors.append('Expected a registered Firebase iOS app configuration.')
if not any('mealsy' in entry.get('CFBundleURLSchemes', []) for entry in info.get('CFBundleURLTypes', [])):
    errors.append('Mealsy invitation URL scheme is missing.')
if not info.get('NSPhotoLibraryUsageDescription'):
    errors.append('Photo picker permission description is missing.')
if info.get('CFBundleIdentifier') not in (None, bundle):
    errors.append('Info.plist overrides the bundle identifier incorrectly.')
if not privacy.get('NSPrivacyAccessedAPITypes'):
    errors.append('App required-reason API declarations are missing.')
scene = info.get('UIApplicationSceneManifest', {})
entries = scene.get('UISceneConfigurations', {}).get('UIWindowSceneSessionRoleApplication', [])
if scene.get('UIApplicationSupportsMultipleScenes') is not False or not any(
    entry.get('UISceneConfigurationName') == '__MAUI_DEFAULT_SCENE_CONFIGURATION__'
    and entry.get('UISceneDelegateClassName') == 'SceneDelegate' for entry in entries):
    errors.append('The single-window MAUI scene configuration is missing.')
delegate = root / 'RecipeApp/Platforms/iOS/SceneDelegate.cs'
if not delegate.is_file() or 'MauiUISceneDelegate' not in delegate.read_text():
    errors.append('The registered MAUI scene delegate is missing.')
if errors:
    raise SystemExit('\n'.join(errors))
print('iOS public configuration checks passed: bundle, project, invitation scheme and privacy metadata.')
print('This does not verify Xcode compatibility, signing, App Store purchases, APNs or device behaviour.')
