#!/usr/bin/env python3
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('simulator_selector', Path(__file__).with_name('select-ios-simulator.py'))
selector = importlib.util.module_from_spec(spec)
spec.loader.exec_module(selector)

def phone(identifier, state='Shutdown', available=True):
    return {'udid': identifier, 'name': 'iPhone ' + identifier, 'state': state, 'isAvailable': available}

class SimulatorSelectionTests(unittest.TestCase):
    def test_preserves_booted_phone_over_newer_shutdown_runtime(self):
        inventory = {'devices': {'.iOS-26-0': [phone('older', 'Booted')], '.iOS-27-0': [phone('newer')]}}
        self.assertEqual(selector.select_device(inventory)['udid'], 'older')

    def test_selects_latest_available_ios_phone_and_excludes_other_platforms(self):
        inventory = {'devices': {'.iOS-26-0': [phone('old')], '.iOS-27-0': [phone('new'), phone('missing', available=False)], '.tvOS-28-0': [phone('tv', 'Booted')]}}
        self.assertEqual(selector.select_device(inventory)['udid'], 'new')

    def test_explicit_choice_never_silently_changes_devices(self):
        inventory = {'devices': {'.iOS-27-0': [phone('chosen'), phone('booted', 'Booted')]}}
        self.assertEqual(selector.select_device(inventory, 'chosen')['udid'], 'chosen')
        with self.assertRaises(ValueError): selector.select_device(inventory, 'not-installed')

    def test_missing_runtime_has_an_actionable_failure(self):
        with self.assertRaisesRegex(ValueError, 'Install an iOS runtime'):
            selector.select_device({'devices': {'.iOS-27-0': [phone('missing', available=False)]}})

if __name__ == '__main__': unittest.main()
