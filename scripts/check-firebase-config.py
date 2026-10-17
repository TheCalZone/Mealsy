#!/usr/bin/env python3
"""Check public mobile/project identifiers without printing API keys or credentials."""
import json
import pathlib
import re
import sys
import xml.etree.ElementTree as ET

root = pathlib.Path(__file__).resolve().parents[1]
config_path = root / 'RecipeApp/Platforms/Android/google-services.json'
config = json.loads(config_path.read_text())
project = config['project_info']['project_id']
app_id = ET.parse(root / 'RecipeApp/RecipeApp.csproj').getroot().find('.//ApplicationId').text
packages = [c['client_info']['android_client_info']['package_name'] for c in config.get('client', []) if 'android_client_info' in c.get('client_info', {})]
errors = []
if app_id not in packages:
    errors.append('google-services.json has no client for the MAUI application package.')
for name in ['HouseholdService.cs', 'FoodFeedService.cs', 'MonetizationConfig.cs']:
    source = (root / 'RecipeApp/Services' / name).read_text()
    endpoints = re.findall(r'https://([a-z0-9-]+\.cloudfunctions\.net)/', source)
    if endpoints and not all(value.endswith("-" + project + ".cloudfunctions.net") for value in endpoints):
        errors.append(f'{name} points at another Firebase project.')
rc = root / '.firebaserc'
if rc.exists() and json.loads(rc.read_text()).get('projects', {}).get('default') not in (None, project):
    errors.append('The Firebase CLI default project differs from the Android configuration.')
print(f'Android package: {app_id}\nFirebase project: {project}')
for error in errors:
    print('FAIL: ' + error)
if not errors:
    print('Public configuration identifiers match. This does not verify live Auth providers, API restrictions, database availability or deployed rules/functions.')
sys.exit(bool(errors))
