#!/usr/bin/env python3
"""Inspect an AAB with a manifest exported by bundletool dump manifest.

This checks ELF alignment inside the bundle, not ZIP alignment of Play-generated
APKs or runtime compatibility. Run bundletool validate and signing checks too.
"""
import argparse, hashlib, json, struct, zipfile
from pathlib import Path
import xml.etree.ElementTree as ET

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('bundle')
p.add_argument('--manifest', required=True)
p.add_argument('--required-target-sdk', type=int, default=36)
p.add_argument('--required-min-sdk', type=int, default=24)
p.add_argument('--output', required=True)
args = p.parse_args()
android = '{http://schemas.android.com/apk/res/android}'
manifest = ET.parse(args.manifest).getroot()
sdk = int(manifest.find('uses-sdk').get(android + 'targetSdkVersion'))
minimum_sdk = int(manifest.find('uses-sdk').get(android + 'minSdkVersion'))
application = manifest.find('application')
libraries = []
with zipfile.ZipFile(args.bundle) as bundle:
    assert bundle.testzip() is None, 'Corrupt bundle archive'
    for entry in bundle.infolist():
        if not any('/lib/' + abi + '/' in entry.filename for abi in ('arm64-v8a', 'x86_64')) or not entry.filename.endswith('.so'):
            continue
        data = bundle.read(entry)
        assert data[:4] == b'\x7fELF' and data[4] == 2, 'Expected ELF64 library'
        endian = '<' if data[5] == 1 else '>'
        offset = struct.unpack_from(endian + 'Q', data, 32)[0]
        size, count = struct.unpack_from(endian + 'HH', data, 54)
        alignments = [struct.unpack_from(endian + 'Q', data, offset + n * size + 48)[0]
                      for n in range(count)
                      if struct.unpack_from(endian + 'I', data, offset + n * size)[0] == 1]
        libraries.append({'library': entry.filename, 'minimum_load_alignment': min(alignments, default=0),
                          'elf_16kb': bool(alignments) and min(alignments) >= 16384})
with open(args.bundle, 'rb') as bundle_file:
    digest = hashlib.file_digest(bundle_file, 'sha256').hexdigest()
report = {
    'sha256': digest,
    'bytes': Path(args.bundle).stat().st_size,
    'package': manifest.get('package'),
    'version_code': manifest.get(android + 'versionCode'),
    'version_name': manifest.get(android + 'versionName'),
    'target_sdk': sdk, 'required_target_sdk': args.required_target_sdk,
    'minimum_sdk': minimum_sdk, 'required_minimum_sdk': args.required_min_sdk,
    'minimum_requirement_met': minimum_sdk >= args.required_min_sdk,
    'target_requirement_met': sdk >= args.required_target_sdk,
    'debuggable': application.get(android + 'debuggable', 'false') == 'true',
    'backup_disabled': application.get(android + 'allowBackup') == 'false',
    'permissions': [e.get(android + 'name') for e in manifest.findall('uses-permission')],
    'native_64bit_library_count': len(libraries),
    'native_elf_16kb_failure_count': sum(not e['elf_16kb'] for e in libraries),
    'native_libraries': libraries,
    'limitations': 'Not Play approval. Does not check generated APK ZIP alignment, signing identity, runtime, store configuration or policy declarations.'
}
Path(args.output).write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps({k: v for k, v in report.items() if k != 'native_libraries'}, indent=2))
