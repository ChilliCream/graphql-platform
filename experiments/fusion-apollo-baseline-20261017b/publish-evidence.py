"""Copy explicit owned evidence, retaining compressed native observations and trace bytes."""
import gzip, hashlib, json, shutil, subprocess
from pathlib import Path
HERE=Path(__file__).resolve().parent
BASE=Path('/workspaces/platform-1/perf/fusion-apollo-baseline-20261017b')
OUT=HERE/'results'
OUT.mkdir(exist_ok=True)
for src in sorted(BASE.rglob('*')):
    if not src.is_file():continue
    relative=src.relative_to(BASE)
    if relative.parts[0] in ['frozen','superseded-unmeasured']:continue
    dst=OUT/relative
    dst.parent.mkdir(parents=True,exist_ok=True)
    if src.suffix=='.nettrace' or src.name.endswith('.speedscope.json'):
        dst=Path(str(dst)+'.gz')
        if dst.exists():continue
        with src.open('rb') as source, dst.open('wb') as raw, gzip.GzipFile(fileobj=raw,mode='wb',mtime=0,compresslevel=6) as target:
            shutil.copyfileobj(source,target)
        verification=hashlib.sha256()
        with gzip.open(dst,'rb') as stream:
            for block in iter(lambda:stream.read(1024*1024),b''):verification.update(block)
        assert verification.hexdigest()==hashlib.sha256(src.read_bytes()).hexdigest()
    else:shutil.copy2(src,dst)
identities=OUT/'binary-metadata';identities.mkdir(exist_ok=True)
for service in (BASE/'frozen').iterdir():
    dest=identities/service.name;dest.mkdir(exist_ok=True)
    for src in service.iterdir():
        if src.name.endswith(('.deps.json','.runtimeconfig.json')):shutil.copy2(src,dest/src.name)
for n in ['binary-sha256.json','configuration.json','reason.json']:
    src=BASE/'superseded-unmeasured'/n
    if src.exists():
        dst=OUT/'superseded-unmeasured'/n;dst.parent.mkdir(exist_ok=True);shutil.copy2(src,dst)
manifest=[]
for p in sorted(OUT.rglob('*')):
    if p.is_file() and p.name!='SHA256SUMS.json':
        manifest.append({'path':str(p.relative_to(OUT)),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(OUT/'SHA256SUMS.json').write_text(json.dumps(manifest,indent=2)+'\n')
assert all(r['bytes']<100_000_000 for r in manifest)
print(json.dumps({'files':len(manifest),'bytes':sum(r['bytes'] for r in manifest),'largest':sorted(manifest,key=lambda r:r['bytes'],reverse=True)[:5]},indent=2))
