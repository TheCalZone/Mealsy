#!/usr/bin/env python3
"""Check public policy navigation under both Firebase root and GitHub project paths."""
from pathlib import Path
from html.parser import HTMLParser
from urllib.parse import urljoin,urlparse
from urllib.request import urlopen
import argparse
root=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--live',action='store_true');args=p.parse_args()
class Links(HTMLParser):
    def __init__(self):super().__init__();self.links=[]
    def handle_starttag(self,tag,attrs):
        for key,value in attrs:
            if key in ('href','src'):self.links.append(value)
for page in ('privacy','delete-account'):
    content=(root/'firebase/desktop'/page/'index.html').read_text()
    assert 'Callum@brightbark.co.uk' in content and 'Pre-release' not in content
    links=Links();links.feed(content)
    for base in ('https://example.test/','https://thecalzone.github.io/Mealsy/'):
        for link in links.links:
            resolved=urlparse(urljoin(base+page+'/',link))
            if resolved.netloc!=urlparse(base).netloc:continue
            prefix=urlparse(base).path
            assert resolved.path.startswith(prefix), 'Link escapes project Pages prefix: '+link
            relative=resolved.path[len(prefix):]
            target=root/'firebase/desktop'/relative
            if relative.endswith('/'):target/='index.html'
            assert target.is_file(), 'Broken policy link: '+link
    if args.live:
        url='https://thecalzone.github.io/Mealsy/'+page+'/'
        with urlopen(url,timeout=20) as response:
            assert response.status==200
            live=response.read(200000).decode()
            assert '<html' in live.lower() and 'Callum@brightbark.co.uk' in live
            assert ('Mealsy privacy notice' if page=='privacy' else 'Delete your Mealsy account') in live
        print('Verified anonymous public HTML:',url)
print('Policy files and navigation pass. Live hosting checked.' if args.live else 'Policy files and navigation pass; live hosting not verified. Use --live after enabling Pages.')
