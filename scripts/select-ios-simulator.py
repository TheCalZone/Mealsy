#!/usr/bin/env python3
"""Select an available iOS simulator from simctl's JSON; never boots a device."""
import argparse
import json
import re
import sys

def select_device(inventory, requested=None):
    devices = []
    for runtime, entries in inventory.get('devices', {}).items():
        if '.iOS-' not in runtime:
            continue
        version = tuple(map(int, re.findall(r'\d+', runtime.split('.iOS-', 1)[1])))
        for device in entries:
            if device.get('isAvailable') is True:
                devices.append((device, version))
    if requested:
        match = next((d for d, _ in devices if d.get('udid') == requested), None)
        if match is None:
            raise ValueError('IOS_SIMULATOR_UDID does not identify an available iOS simulator.')
        return match
    phones = [(d, v) for d, v in devices if d.get('name', '').startswith('iPhone')]
    candidates = phones or devices
    if not candidates:
        raise ValueError('No available iOS simulator. Install an iOS runtime/device in Xcode first.')
    return max(candidates, key=lambda entry: (entry[0].get('state') == 'Booted', entry[1], entry[0].get('name', '')))[0]

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('inventory')
    parser.add_argument('--udid')
    args = parser.parse_args()
    try:
        with open(args.inventory) as inventory_file:
            device = select_device(json.load(inventory_file), args.udid)
    except (ValueError, OSError) as error:
        raise SystemExit(str(error))
    print('Selected simulator: ' + device['name'], file=sys.stderr)
    print(device['udid'])
