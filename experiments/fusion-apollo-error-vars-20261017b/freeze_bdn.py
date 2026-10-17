#!/usr/bin/env python3
import hashlib
import json
from pathlib import Path
import shutil

HERE = Path(__file__).resolve().parent
ROOT = Path('/workspaces/platform-1/perf/fusion-apollo-error-vars-20261017b-43a8dca2')
OUTPUT = ROOT / 'frozen-bdn'
BUILD = HERE / 'Bdn/bin/Release/net10.0'
COMMON = ROOT / 'frozen-fixed-heaps/baseline'
ENGINE = 'HotChocolate.Fusion.Execution'
HARNESS = 'HotChocolate.Fusion.Execution.Benchmarks'
ARMS = {
    'B': COMMON,
    'C2': ROOT / 'frozen-fixed-heaps/candidate',
    'C3': Path('/workspaces/platform-1/perf/fusion-apollo-duplicate-paths-20261017b/frozen/gateway'),
}
EXPECTED = {
    'B': '9c436477e63d9c7f5c8668dcd75fec9771f500a5e71b14a1f02dd0bcf8fe6c5b',
    'C2': '244ad610bfe4ab6152885955e22776124510acb344f8c783a3f3a17260878301',
    'C3': '359a421c3387637ab022c658266252616e0ab58e262c72ad57a47c6ca6ae50cf',
}
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()
if OUTPUT.exists():
    raise FileExistsError(OUTPUT)
shutil.copytree(BUILD, OUTPUT / 'B')
for path in COMMON.rglob('*'):
    if path.is_file() and not path.name.startswith(HARNESS):
        target = OUTPUT / 'B' / path.relative_to(COMMON)
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(path, target)
for arm, source in ARMS.items():
    if arm != 'B':
        shutil.copytree(OUTPUT / 'B', OUTPUT / arm)
    for suffix in ['.dll', '.pdb']:
        shutil.copyfile(source / (ENGINE + suffix), OUTPUT / arm / (ENGINE + suffix))
    if sha(OUTPUT / arm / (ENGINE + '.dll')) != EXPECTED[arm]:
        raise ValueError('Engine identity changed: ' + arm)
files = {str(p.relative_to(OUTPUT)): sha(p) for p in sorted(OUTPUT.rglob('*')) if p.is_file()}
differences = {}
for arm in ['C2', 'C3']:
    differences[arm] = sorted(str(p.relative_to(OUTPUT / 'B')) for p in (OUTPUT / 'B').rglob('*')
                              if p.is_file() and sha(p) != sha(OUTPUT / arm / p.relative_to(OUTPUT / 'B')))
    if differences[arm] != [ENGINE + '.dll', ENGINE + '.pdb']:
        raise ValueError(differences)
sources = list((HERE / 'Bdn').glob('*.cs')) + [HERE / 'Bdn/Bdn.csproj', HERE / 'Micro/SchemaFixture.cs', HERE / 'run_bdn.py', HERE / 'freeze_bdn.py']
manifest = {
    'base': '43a8dca2bd479ddc411d832ed97e42d9fcc8bd85',
    'sourceHashes': {str(p.relative_to(HERE)): sha(p) for p in sources},
    'files': files,
    'variantDifferences': [ENGINE + '.dll', ENGINE + '.pdb'],
    'differencesByArm': differences,
    'package': 'BenchmarkDotNet 0.15.8',
    'method': 'InProcessEmit in fresh isolated outer process; default GC; no MemoryDiagnoser',
    'schedule': ['B', 'C2', 'C3', 'B', 'C3', 'C2', 'B'],
    'cases': ['ordinary', 'batch9', 'duplicate96-to1'],
    'prewarmSecondsPerCase': 3, 'warmupIterations': 3, 'measurementIterations': 12,
    'operationsPerInvocation': 1000, 'invocationCount': 1, 'unrollFactor': 1,
    'outlierRemoval': False, 'forceGc': False,
    'childDeadlineSeconds': 60, 'campaignDeadlineSeconds': 285, 'cleanupMaximumSeconds': 10,
    'provenance': 'One BDN host plus established common micro dependency set; exact prior measured engines only vary.'
}
(OUTPUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
print(json.dumps({'path': str(OUTPUT), 'manifestSha256': sha(OUTPUT / 'manifest.json'), 'differences': differences}, indent=2))
