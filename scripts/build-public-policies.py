#!/usr/bin/env python3
"""Build only public policy assets; never publish the app, Firebase config or credentials."""
from pathlib import Path
import argparse,shutil
p=argparse.ArgumentParser();p.add_argument('--output',required=True);args=p.parse_args()
root=Path(__file__).resolve().parents[1];out=Path(args.output).resolve()
if out==root or root in out.parents and not out.name.startswith('policies-site'):
    raise SystemExit('Choose a separate policy-site output directory.')
out.mkdir(parents=True,exist_ok=True)
for relative in ('privacy/index.html','privacy/legal.css','delete-account/index.html'):
    source=root/'firebase/desktop'/relative
    destination=out/relative;destination.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,destination)
(out/'.nojekyll').write_text('')
(out/'index.html').write_text('<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Mealsy privacy and support</title><link rel="stylesheet" href="privacy/legal.css"></head><body><main><h1>Mealsy privacy and support</h1><p><a href="privacy/">Privacy policy</a></p><p><a href="delete-account/">Account and data deletion</a></p><p>Contact <a href="mailto:Callum@brightbark.co.uk">Callum@brightbark.co.uk</a>.</p></main></body></html>')
allowed={'index.html','.nojekyll','privacy/index.html','privacy/legal.css','delete-account/index.html'}
actual={f.relative_to(out).as_posix() for f in out.rglob('*') if f.is_file() and '.git' not in f.parts}
if actual!=allowed:raise SystemExit('Unexpected files in output; use a clean directory.')
print('Public policy site built with exactly five allowlisted files.')
