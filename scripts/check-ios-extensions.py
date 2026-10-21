#!/usr/bin/env python3
"""Cross-component iOS extension configuration checks, not a native compiler."""
from pathlib import Path
import plistlib
import re
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
def plist(path):
    return plistlib.loads((root / path).read_bytes())
project = ET.parse(root / 'RecipeApp/RecipeApp.csproj').getroot()
bundle = project.findtext('./PropertyGroup/ApplicationId')
group = 'group.' + bundle
host = plist('RecipeApp/Platforms/iOS/Entitlements.plist')
share = plist('native/ios/MealsyShare/MealsyShare.entitlements')
assert host == share == {'com.apple.security.application-groups': [group]}, 'App Group mismatch'
yaml = (root / 'native/ios/project.yml').read_text()
for suffix in ('share', 'widget'):
    assert f'PRODUCT_BUNDLE_IDENTIFIER: {bundle}.{suffix}' in yaml, 'Extension bundle ID mismatch'
for name in ('MealsyShare', 'MealsyWidget'):
    info = plist(f'native/ios/{name}/Info.plist')
    assert info['CFBundleVersion'] == '$(CURRENT_PROJECT_VERSION)'
    assert info['CFBundleShortVersionString'] == '$(MARKETING_VERSION)'
    assert any(item.findtext('Name') == name for item in project.findall('./ItemGroup/AdditionalAppExtensions')), 'Missing MAUI embed item'
info = plist('native/ios/MealsyShare/Info.plist')
assert info['NSExtension']['NSExtensionAttributes']['NSExtensionActivationRule'] == {'NSExtensionActivationSupportsWebURLWithMaxCount': 1}
assert info['NSExtension']['NSExtensionPointIdentifier'] == 'com.apple.share-services'
assert plist('native/ios/MealsyWidget/Info.plist')['NSExtension']['NSExtensionPointIdentifier'] == 'com.apple.widgetkit-extension'
bridge = (root / 'RecipeApp/Platforms/iOS/ExtensionNavigation.cs').read_text()
swift = (root / 'native/ios/MealsyShare/ShareViewController.swift').read_text()
assert group in bridge and group in swift
assert 'RecipeInbox' in bridge and 'RecipeInbox' in swift
widget = (root / 'native/ios/MealsyWidget/MealsyWidget.swift').read_text()
links = set(re.findall(r'mealsy://widget/[a-z]+', widget))
assert links == {'mealsy://widget/add', 'mealsy://widget/open', 'mealsy://widget/configure'}
assert all(link in bridge for link in links), 'Widget URL has no app handler'
assert 'IOS_EXTENSIONS' in (root / 'RecipeApp/Services/ClientPlatform.cs').read_text()
print('iOS extension metadata checks passed: App Group, bundle IDs, versions, embed items and widget routes.')
print('Native Swift/MAUI build, signing, App Group access and device interaction remain Mac checks.')
