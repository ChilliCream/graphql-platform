#!/usr/bin/env python3
from pathlib import Path
import hashlib
import json
import shutil
HERE = Path(__file__).resolve().parent
ROOT = Path('/workspaces/platform-1/perf/fusion-apollo-transport-async-20261017b')
OUTPUT = ROOT / 'frozen-bdn'
COMMON = Path('/workspaces/platform-1/perf/fusion-apollo-error-vars-20261017b-43a8dca2/frozen-bdn/B')
BUILD = HERE / 'Bdn/bin/Release/net10.0'
ENGINE = 'HotChocolate.Fusion.Execution'
HARNESS = 'HotChocolate.Fusion.Execution.Benchmarks'
CANDIDATE = HERE.parents[1] / 'perf/fusion-apollo-transport-async-20261017b-43a8dca2/frozen-engine'
ARMS = {'B': COMMON, 'C12': CANDIDATE}
EXPECTED = {'B': '9c436477e63d9c7f5c8668dcd75fec9771f500a5e71b14a1f02dd0bcf8fe6c5b',
 'C12': 'a5593442501444e2e6964065a5ab9b51666ecd38fbc096f79e67a5a2ce7adf83'}
def sha(path):
 return hashlib.sha256(path.read_bytes()).hexdigest()
if OUTPUT.exists():
 raise FileExistsError(OUTPUT)
shutil.copytree(COMMON, OUTPUT / 'B')
for suffix in ['.dll', '.pdb', '.deps.json', '.runtimeconfig.json']:
 shutil.copyfile(BUILD / (HARNESS + suffix), OUTPUT / 'B' / (HARNESS + suffix))
for arm, source in ARMS.items():
 if arm != 'B':
  shutil.copytree(OUTPUT / 'B', OUTPUT / arm)
 for suffix in ['.dll', '.pdb']:
  shutil.copyfile(source / (ENGINE + suffix), OUTPUT / arm / (ENGINE + suffix))
 assert sha(OUTPUT / arm / (ENGINE + '.dll')) == EXPECTED[arm]
files = {str(p.relative_to(OUTPUT)): sha(p) for p in sorted(OUTPUT.rglob('*')) if p.is_file()}
changes = {}
for arm in ['C12']:
 changes[arm] = sorted(str(p.relative_to(OUTPUT / 'B')) for p in (OUTPUT / 'B').rglob('*') if p.is_file() and sha(p) != sha(OUTPUT / arm / p.relative_to(OUTPUT / 'B')))
 assert changes[arm] == [ENGINE + '.dll', ENGINE + '.pdb']
sources = [HERE / 'run_bdn.py', HERE / 'freeze_bdn.py', HERE / 'Bdn/Bdn.csproj'] + list((HERE / 'Bdn').glob('*.cs'))
manifest = {'base':'43a8dca2bd479ddc411d832ed97e42d9fcc8bd85',
 'files':files, 'variantDifferences':[ENGINE + '.dll', ENGINE + '.pdb'], 'differencesByArm': changes,
 'sourceHashes':{str(p.relative_to(HERE)):sha(p) for p in sources},
 'cases':['ordinary','async-ordinary','async-alias2','async-custom'],
 'allocationSchedule':['B','C12','B'], 'timingSchedule':['B','C12','B','C12','B'],
 'allocationPolicy':{'DOTNET_GCHeapCount':'4','DOTNET_GCDynamicAdaptationMode':'0','mode':0,'noGcBudget':536870912,'calibrationBytes':523240,'blocks':3,'operationsPerBlock':1000},
 'timingPolicy':{'runtimeOverrides':{},'mode':1,'warmSeconds':3,'warmupIterations':3,'actualIterations':12,'operationsPerInvocation':1000,'outlierRemoval':False,'nativeMemoryDiagnoser':True},
 'childDeadlineSeconds':60,'campaignDeadlineSeconds':285,'cleanupMaximumSeconds':10,
 'scope':'Actual sealed default HTTP client and HttpSourceSchemaClient serialization, response splitting, complete disposal and arena ownership; native MemoryDiagnoser/default-GC timing first, separate calibrated process allocation afterward; no network or process CPU claim.'}
(OUTPUT / 'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps({'manifestSha256':sha(OUTPUT/'manifest.json'),'runnerSha256':sha(HERE/'run_bdn.py'),'files':len(files),'changes':changes},indent=2))
