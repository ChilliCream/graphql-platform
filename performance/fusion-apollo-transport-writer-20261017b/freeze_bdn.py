#!/usr/bin/env python3
from pathlib import Path
import hashlib
import json
import shutil

HERE = Path(__file__).resolve().parent
ROOT = Path('/workspaces/platform-1/perf/fusion-apollo-shared-client-20261017b')
OUTPUT = ROOT / 'frozen-bdn'
COMMON_ROOT = Path('/workspaces/platform-1/perf/fusion-apollo-alias-items-20261017b/frozen-bdn')
COMMON = COMMON_ROOT / 'B'
ENGINE = 'HotChocolate.Fusion.Execution'
ARMS = {
    'B': COMMON,
    'C11': Path('/workspaces/platform-1/perf/fusion-apollo-transport-writer-20261017b/overlay'),
    'C13': Path('/workspaces/platform-1/.worktrees/fusion-apollo-alias-cursors-20261017b/src/HotChocolate/Fusion/src/Fusion.Execution/bin/Release/net10.0'),
}
EXPECTED = {
    'B': '9c436477e63d9c7f5c8668dcd75fec9771f500a5e71b14a1f02dd0bcf8fe6c5b',
    'C11': '2d68df9b88a4abc3bedd2a9d671653aa758e8475b2ce342f4b8807331c755084',
    'C13': 'f06a867f5fbfa552edd2904691c74ea818b23635c537272aeec5cb2f4f254b14',
}

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

if OUTPUT.exists():
    raise FileExistsError(OUTPUT)
source_manifest = json.loads((COMMON_ROOT / 'manifest.json').read_text())
for relative, expected in source_manifest['files'].items():
    if relative.startswith('B/'):
        assert sha(COMMON_ROOT / relative) == expected, relative
for arm, source in ARMS.items():
    shutil.copytree(COMMON, OUTPUT / arm)
    for suffix in ('.dll', '.pdb'):
        shutil.copyfile(source / (ENGINE + suffix), OUTPUT / arm / (ENGINE + suffix))
    assert sha(OUTPUT / arm / (ENGINE + '.dll')) == EXPECTED[arm]
files = {str(p.relative_to(OUTPUT)): sha(p) for p in sorted(OUTPUT.rglob('*')) if p.is_file()}
changes = {}
baseline_names = {str(p.relative_to(OUTPUT / 'B')) for p in (OUTPUT / 'B').rglob('*') if p.is_file()}
for arm in ('C11', 'C13'):
    arm_names = {str(p.relative_to(OUTPUT / arm)) for p in (OUTPUT / arm).rglob('*') if p.is_file()}
    assert baseline_names == arm_names
    changes[arm] = sorted(name for name in baseline_names if sha(OUTPUT / 'B' / name) != sha(OUTPUT / arm / name))
    assert changes[arm] == [ENGINE + '.dll', ENGINE + '.pdb']
sources = [HERE / 'run_bdn.py', HERE / 'freeze_bdn.py', HERE / 'Bdn/Bdn.csproj'] + list((HERE / 'Bdn').glob('*.cs'))
manifest = {
    'base': '43a8dca2bd479ddc411d832ed97e42d9fcc8bd85',
    'files': files,
    'variantDifferences': [ENGINE + '.dll', ENGINE + '.pdb'],
    'differencesByArm': changes,
    'sourceHashes': {str(p.relative_to(HERE)): sha(p) for p in sources},
    'commonManifestSha256': sha(COMMON_ROOT / 'manifest.json'),
    'commonHelperSourceCommit': 'd943238c00635b08036c279fe6ae710444bb1735',
    'candidateSourceCommits': {'C11': '1b2eefe1b2411a8266e6521001d7aa6af13bd50f', 'C13': '8291f82de4f371e913d6fdd443ceec262ed18105'},
    'cases': ['ordinary', 'alias2', 'alias-sequence10'],
    'timingSchedule': ['B', 'C11', 'C13', 'B', 'C13', 'C11', 'B'],
    'allocationSchedule': ['B', 'C11', 'C13', 'B'],
    'allocationPolicy': {'DOTNET_GCHeapCount': '4', 'DOTNET_GCDynamicAdaptationMode': '0', 'mode': 0, 'noGcBudget': 536870912, 'calibrationBytes': 523240, 'blocks': 3, 'operationsPerBlock': 1000},
    'timingPolicy': {'runtimeOverrides': {}, 'mode': 1, 'warmSeconds': 3, 'warmupIterations': 3, 'actualIterations': 12, 'operationsPerInvocation': 1000, 'outlierRemoval': False, 'nativeMemoryDiagnoser': True},
    'childDeadlineSeconds': 60,
    'campaignDeadlineSeconds': 285,
    'cleanupMaximumSeconds': 10,
    'scope': 'Exact C4 actual production client helper, independent C11/C13 engines. C13 measures two/three-item splits; 256/257/1024 fallback coverage is correctness only. Native MemoryDiagnoser/default-GC elapsed first, separately calibrated process allocation afterward. No network or CPU claim.',
}
(OUTPUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
print(json.dumps({'manifestSha256': sha(OUTPUT / 'manifest.json'), 'runnerSha256': sha(HERE / 'run_bdn.py'), 'files': len(files), 'changes': changes}, indent=2))
