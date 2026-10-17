#!/usr/bin/env python3
import hashlib
import json
from pathlib import Path
import shutil

HERE = Path(__file__).resolve().parent
ROOT = Path('/workspaces/platform-1/perf/fusion-apollo-source-stream-20261017b-43a8dca2')
OUTPUT = ROOT / 'frozen-shared-md-checked'
ORIGINAL = ROOT / 'frozen'
BUILD = HERE / 'Bdn/bin/Release/net10.0'
C10 = Path('/workspaces/platform-1/.worktrees/fusion-apollo-contiguous-source-20261017b/src/HotChocolate/Fusion/src/Fusion.Execution/bin/Release/net10.0')
ENGINE = 'HotChocolate.Fusion.Execution'
HARNESS = 'HotChocolate.Fusion.Execution.Benchmarks'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


if OUTPUT.exists():
    raise FileExistsError(OUTPUT)
shutil.copytree(ORIGINAL / 'B', OUTPUT / 'B')
for path in BUILD.glob(HARNESS + '.*'):
    if path.is_file():
        shutil.copyfile(path, OUTPUT / 'B' / path.name)
for arm, source in [('C7', ORIGINAL / 'C7'), ('C10', C10)]:
    shutil.copytree(OUTPUT / 'B', OUTPUT / arm)
    for suffix in ['.dll', '.pdb']:
        shutil.copyfile(source / (ENGINE + suffix), OUTPUT / arm / (ENGINE + suffix))
expected = {
    'B': '9c436477e63d9c7f5c8668dcd75fec9771f500a5e71b14a1f02dd0bcf8fe6c5b',
    'C7': 'c651030acc2ca721ee56a9bb0d85290f432cfeeb675d5b45f359bf33117be052'
}
for arm, digest in expected.items():
    if sha(OUTPUT / arm / (ENGINE + '.dll')) != digest:
        raise ValueError('Previously frozen engine changed: ' + arm)
files = {str(p.relative_to(OUTPUT)): sha(p) for p in sorted(OUTPUT.rglob('*')) if p.is_file()}
differences = {}
for arm in ['C7', 'C10']:
    differences[arm] = sorted(str(p.relative_to(OUTPUT / 'B')) for p in (OUTPUT / 'B').rglob('*')
                              if p.is_file() and sha(p) != sha(OUTPUT / arm / p.relative_to(OUTPUT / 'B')))
    if differences[arm] != [ENGINE + '.dll', ENGINE + '.pdb']:
        raise ValueError(differences)
sources = [HERE / 'Bdn/Program.cs', HERE / 'Bdn/Bdn.csproj', HERE / 'freeze_shared.py', HERE / 'run_bdn.py']
schedule = [['allocate', arm] for arm in ['B', 'C7', 'C10', 'B']]
schedule += [['benchmark', arm] for arm in ['B', 'C7', 'C10', 'B', 'C10', 'C7', 'B']]
manifest = {
    'base': '43a8dca2bd479ddc411d832ed97e42d9fcc8bd85',
    'sourceHashes': {str(p.relative_to(HERE)): sha(p) for p in sources},
    'files': files,
    'differencesByArm': differences,
    'cases': ['known128', 'unknown4096', 'unknown32768'],
    'schedule': schedule,
    'package': 'BenchmarkDotNet 0.15.8',
    'memoryDiagnoser': True,
    'method': 'InProcessEmit inside isolated arm processes; default GC timing plus native MemoryDiagnoser',
    'allocationMethod': 'Separate fixed-four-heap 512MiB no-GC region; zero collections and successful exit; synchronous current-thread allocation with exact known-allocation calibration before/after each block',
    'prewarmSecondsPerCase': 3,
    'warmupIterations': 3,
    'measurementIterations': 12,
    'operationsPerInvocation': 1000,
    'invocationCount': 1,
    'unrollFactor': 1,
    'outlierRemoval': False,
    'forceGc': False,
    'childDeadlineSeconds': 75,
    'campaignDeadlineSeconds': 420,
    'cleanupMaximumSeconds': 10,
    'provenance': 'Original unmeasured C7 freeze retained. User explicitly requested MemoryDiagnoser before any run; rebuilt only common fixture and retained exact B/C7 engines. C10 independent baseline prototype shares applicable response cases and matched controls.'
}
(OUTPUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
print(json.dumps({'path': str(OUTPUT), 'manifestSha256': sha(OUTPUT / 'manifest.json'),
                  'runnerSha256': sha(HERE / 'run_bdn.py'), 'differences': differences,
                  'engineHashes': {arm: sha(OUTPUT / arm / (ENGINE + '.dll')) for arm in ['B', 'C7', 'C10']}}, indent=2))
