#!/usr/bin/env python3
"""Inspect a test APK for concrete release blockers; not a Play policy certification."""
import argparse,json,struct,subprocess,zipfile
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('apk');p.add_argument('--aapt',required=True);p.add_argument('--output');p.add_argument('--required-target-sdk',type=int,default=36);p.add_argument('--required-min-sdk',type=int,default=24);args=p.parse_args()
badging=subprocess.check_output([args.aapt,'dump','badging',args.apk],text=True)
manifest=subprocess.check_output([args.aapt,'dump','xmltree',args.apk,'AndroidManifest.xml'],text=True)
permissions=subprocess.check_output([args.aapt,'dump','permissions',args.apk],text=True)
import re
sdk=int(re.search(r"targetSdkVersion:'(\d+)'",badging)[1])
minimum=int(re.search(r"sdkVersion:'(\d+)'",badging)[1])
failures=[];native=[]
with zipfile.ZipFile(args.apk) as z:
 assert z.testzip() is None
 for entry in z.infolist():
  if not entry.filename.startswith(('lib/arm64-v8a/','lib/x86_64/')) or not entry.filename.endswith('.so'):continue
  data=z.read(entry);assert data[:4]==b'\x7fELF'
  endian='<' if data[5]==1 else '>';is64=data[4]==2
  if not is64:continue
  offset=struct.unpack_from(endian+'Q',data,32)[0];size,count=struct.unpack_from(endian+'HH',data,54)
  alignments=[]
  for i in range(count):
   pos=offset+i*size
   if struct.unpack_from(endian+'I',data,pos)[0]==1:alignments.append(struct.unpack_from(endian+'Q',data,pos+48)[0])
  with open(args.apk,'rb') as f:
   f.seek(entry.header_offset+26);name_len,extra_len=struct.unpack('<HH',f.read(4))
  payload=entry.header_offset+30+name_len+extra_len
  valid=bool(alignments) and min(alignments)>=16384
  zip_ok=entry.compress_type!=zipfile.ZIP_STORED or payload%16384==0
  native.append({'library':entry.filename,'minimum_load_alignment':min(alignments,default=0),'elf_16kb':valid,'zip_16kb':zip_ok})
  if not valid or not zip_ok:failures.append(entry.filename)
report={'target_sdk':sdk,'required_target_sdk':args.required_target_sdk,'target_requirement_met':sdk>=args.required_target_sdk,'minimum_sdk':minimum,'required_minimum_sdk':args.required_min_sdk,'minimum_requirement_met':minimum>=args.required_min_sdk,'advertising_id_permission':'com.google.android.gms.permission.AD_ID' in permissions,'backup_disabled':bool(re.search(r'android:allowBackup[^\n]*=(?:\(type 0x12\))?0x0\b',manifest)), 'debuggable':bool(re.search(r'android:debuggable[^\n]*0xffffffff',manifest)),'native_64bit_library_count':len(native),'native_16kb_failures':failures,'native_libraries':native,'limitations':'ELF/ZIP inspection does not replace 16 KB device and final Play-generated APK checks. Signing and store configuration require separate verification.'}
text=json.dumps(report,indent=2)
if args.output:Path(args.output).write_text(text+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='native_libraries'},indent=2))
