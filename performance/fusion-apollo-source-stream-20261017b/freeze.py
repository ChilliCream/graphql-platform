#!/usr/bin/env python3
import hashlib
import json
from pathlib import Path
import shutil

HERE = Path(__file__).resolve().parent
ROOT = Path('/workspaces/platform-1/perf/fusion-apollo-source-stream-20261017b-43a8dca2')
OUTPUT = ROOT / 'frozen'
BUILD = HERE / 'Bdn/bin/Release/net10.0'
CANDIDATE = HERE.parents[1] / 'src/HotChocolate/Fusion/src/Fusion.Execution/bin/Release/net10.0'
COMMON = Path('/workspaces/platform-1/perf/fusion-apollo-error-vars-20261017b-43a8dca2/frozen-bdn/B')
ENGINE = 'HotChocolate.Fusion.Execution'
HARNESS = 'HotChocolate.Fusion.Execution.Benchmarks'


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
shutil.copytree(OUTPUT / 'B', OUTPUT / 'C7')
for suffix in ['.dll', '.pdb']:
    shutil.copyfile(CANDIDATE / (ENGINE + suffix), OUTPUT / 'C7' / (ENGINE + suffix))
if sha(OUTPUT / 'B' / (ENGINE + '.dll')) != '9c436477e63d9c7f5c8668dcd75fec9771f500a5e71b14a1f02dd0bcf8fe6c5b':
    raise ValueError('Baseline identity changed.')
files = {str(p.relative_to(OUTPUT)): sha(p) for p in sorted(OUTPUT.rglob('*')) if p.is_file()}
differences = sorted(str(p.relative_to(OUTPUT / 'B')) for p in (OUTPUT / 'B').rglob('*')
                     if p.is_file() and sha(p) != sha(OUTPUT / 'C7' / p.relative_to(OUTPUT / 'B')))
if differences != [ENGINE + '.dll', ENGINE + '.pdb']:
    raise ValueError(differences)
sources = list((HERE / 'Bdn').glob('*.cs')) + [HERE / 'Bdn/Bdn.csproj', HERE / 'freeze.py']
manifest = {
    'base': '43a8dca2bd479ddc411d832ed97e42d9fcc8bd85',
    'sourceHashes': {str(p.relative_to(HERE)): sha(p) for p in sources},
    'files': files,
    'variantDifferences': differences,
    'cases': ['known128', 'unknown4096', 'unknown32768'],
    'package': 'BenchmarkDotNet 0.15.8',
    'method': 'InProcessEmit inside isolated arm process; default GC; no MemoryDiagnoser',
    'allocationMethod': 'Synchronous complete production reads; fixed four heaps; 512MiB no-GC regions; zero collections; successful region exit; current-thread bytes with exact 523240-byte calibrations before/after every block',
    'prewarmSecondsPerCase': 3,
    'warmupIterations': 3,
    'measurementIterations': 12,
    'operationsPerInvocation': 1000,
    'invocationCount': 1,
    'unrollFactor': 1,
    'outlierRemoval': False,
    'forceGc': False,
    'provenance': 'Own fixture and candidate engine; exact shared baseline engines/dependencies overlaid, only Execution DLL/PDB differ.'
}
(OUTPUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
print(json.dumps({'path': str(OUTPUT), 'manifestSha256': sha(OUTPUT / 'manifest.json'), 'differences': differences,
                  'engineHashes': {arm: sha(OUTPUT / arm / (ENGINE + '.dll')) for arm in ['B', 'C7']}}, indent=2))
