#!/usr/bin/env python3
"""Read-only Mac/iOS readiness report. Does not install tools or access signing keys."""
from pathlib import Path
import platform
import shutil
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]


def command(args):
    try:
        result = subprocess.run(args, cwd=ROOT, text=True, capture_output=True, timeout=45)
        print('$ ' + ' '.join(args))
        print((result.stdout or result.stderr).strip())
        return result.returncode == 0
    except (OSError, subprocess.TimeoutExpired):
        print('Unavailable: ' + args[0])
        return False


def main():
    project = ET.parse(ROOT / 'RecipeApp/RecipeApp.csproj').getroot()
    frameworks = [node.text for node in project.findall('./PropertyGroup/TargetFrameworks')]
    print('Mealsy iOS readiness — source metadata is not a native build result.')
    print('Project targets: ' + ', '.join(frameworks))
    legacy = any('net8.0-ios' in value for value in frameworks)
    if legacy:
        print('RELEASE BLOCKER: .NET/MAUI 8 and legacy Firebase iOS bindings need migration.')
        print('Use a supported .NET/MAUI 10 toolchain and matching Xcode/native dependencies.')
    if platform.system() != 'Darwin':
        print('Run this script on your Mac for Xcode, SDK and workload diagnostics.')
        print('Native iOS build, signing and simulator tests cannot run on this host.')
        return 2
    ready = True
    for args in (['xcode-select', '-p'], ['xcodebuild', '-version'],
                 ['xcrun', '--sdk', 'iphoneos', '--show-sdk-version'],
                 ['dotnet', '--list-sdks'], ['dotnet', 'workload', 'list']):
        if not shutil.which(args[0]) or not command(args):
            ready = False
    print('Match Xcode to the chosen .NET iOS workload release; newest is not automatically compatible.')
    print('Apple requires Xcode 26+/iOS SDK 26+ for uploads. Xcode 27 also requires UIScene lifecycle support.')
    print('No certificates, provisioning profiles, private keys or account tokens were inspected.')
    return 0 if ready and not legacy else 2


if __name__ == '__main__':
    raise SystemExit(main())
