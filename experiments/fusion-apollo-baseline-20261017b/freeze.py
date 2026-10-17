import hashlib, json, os, shutil, subprocess
from pathlib import Path
HERE=Path(__file__).resolve().parent
OUT=Path('/workspaces/platform-1/perf/fusion-apollo-baseline-20261017b')
FROZEN=OUT/'frozen'
assert not FROZEN.exists(), 'Frozen binaries must never be overwritten'
FROZEN.mkdir()
for name,src in [('gateway',HERE/'host/bin/Release/net10.0')]+[(n,HERE/f'fixture/subgraphs-net/eShop.{n}/bin/Release/net10.0') for n in ['Accounts','Inventory','Products','Reviews']]:
    shutil.copytree(src,FROZEN/name)
    if name=='gateway':
        for f in ['appsettings.json','gateway.far']:
            shutil.copy2(HERE/'host'/f,FROZEN/name/f)
    else:
        shutil.copy2(HERE/f'fixture/subgraphs-net/eShop.{name}/appsettings.json',FROZEN/name/'appsettings.json')
manifest={str(p.relative_to(FROZEN)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(FROZEN.rglob('*')) if p.is_file()}
(OUT/'binary-sha256.json').write_text(json.dumps(manifest,indent=2)+'\n')
metadata={'platform':subprocess.check_output(['git','rev-parse','HEAD'],cwd=HERE,text=True).strip(),'benchmark':'941599e4e27719084189e12d737e147069fdf794','runtime':'10.0.12','platform_sdk':'11.0.100-rc.1.26425.128','subgraph_sdk':'10.0.401','subgraph_packages':'16.6.8','security':'AddGraphQLGateway(disableDefaultSecurity: true), benchmark generated FUSION_16_7_OR_GREATER','logging':'None, source client RemoveAllLoggers','min_threads':[256,256],'max_connections_per_server':256,'gc':'framework Web SDK default server GC, no overrides','gateway_affinity':'2-3,10-11','subgraph_affinity':'4-7,12-15','generator_affinity':'1,9','observer_affinity':'0,8'}
(OUT/'configuration.json').write_text(json.dumps(metadata,indent=2)+'\n')
print(OUT/'binary-sha256.json')
