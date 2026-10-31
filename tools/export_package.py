#!/usr/bin/env python3
"""Export only Assets/CG2SkillPanel with its existing Unity GUIDs."""
from pathlib import Path
import gzip,hashlib,io,json,re,tarfile,zipfile
from validate_assets import validate
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Assets/CG2SkillPanel'
VERSION='0.1.0'
report=validate()
DIST=ROOT/'dist';DIST.mkdir(exist_ok=True)
package=DIST/f'CG2SkillPanel-{VERSION}.unitypackage'
assets=sorted(p for p in BASE.rglob('*') if p.is_dir() or (p.is_file() and not p.name.endswith('.meta')))
assets.insert(0,BASE)
with package.open('wb') as raw:
    with gzip.GzipFile(fileobj=raw,mode='wb',mtime=0,filename='') as gz:
        with tarfile.open(fileobj=gz,mode='w') as archive:
            for p in assets:
                meta=p.with_name(p.name+'.meta').read_bytes()
                guid=re.search(rb'^guid: ([0-9a-f]{32})$',meta,re.M).group(1).decode()
                entries=[('pathname',(p.relative_to(ROOT).as_posix()+'\n').encode()),('asset.meta',meta)]
                if p.is_file():entries.append(('asset',p.read_bytes()))
                for name,data in entries:
                    info=tarfile.TarInfo(guid+'/'+name);info.size=len(data);info.mode=0o644;info.mtime=0
                    archive.addfile(info,io.BytesIO(data))
zip_path=DIST/f'CG2SkillPanel-{VERSION}-Assets.zip'
with zipfile.ZipFile(zip_path,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
    for p in sorted((ROOT/'Assets').rglob('*')):
        if p.is_file():
            info=zipfile.ZipInfo(p.relative_to(ROOT).as_posix(),date_time=(2026,11,1,0,0,0))
            info.compress_type=zipfile.ZIP_DEFLATED;info.external_attr=0o644<<16
            z.writestr(info,p.read_bytes())
checksums=[]
for p in [package,zip_path]:
    checksums.append(hashlib.sha256(p.read_bytes()).hexdigest()+'  '+p.name)
(DIST/'SHA256SUMS').write_text('\n'.join(checksums)+'\n')
with zipfile.ZipFile(zip_path) as z:
    assert z.testzip() is None
    for p in [p for p in BASE.rglob('*') if p.is_file()]:assert z.read(p.relative_to(ROOT).as_posix())==p.read_bytes()
with tarfile.open(package) as t:
    for p in assets:
        if not p.is_file():continue
        guid=re.search(r'^guid: ([0-9a-f]{32})$',p.with_name(p.name+'.meta').read_text(),re.M).group(1)
        assert t.extractfile(guid+'/asset').read()==p.read_bytes()
print(json.dumps(dict(validation=report,artifacts={p.name:p.stat().st_size for p in [package,zip_path]}),ensure_ascii=False,indent=2))
