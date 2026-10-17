#!/usr/bin/env python3
from pathlib import Path
import hashlib
import json
import shutil

HERE = Path(__file__).resolve().parent
ROOT = Path('/workspaces/platform-1/perf/fusion-apollo-shared-variable-slots-20261017b')
OUTPUT = ROOT / 'frozen-bdn'
COMMON_ROOT = Path('/workspaces/platform-1/perf/fusion-apollo-transport-async-20261017b/frozen-bdn')
COMMON = COMMON_ROOT / 'B'
ENGINE = 'HotChocolate.Fusion.Execution'
ARMS = {
    'B': COMMON,
    'C14': Path('/workspaces/platform-1/perf/fusion-apollo-shared-variables-20261017b/overlay'),
    'C15': Path('/workspaces/platform-1/.worktrees/fusion-apollo-result-slots-20261017b/src/HotChocolate/Fusion/src/Fusion.Execution/bin/Release/net10.0'),
}
EXPECTED = {
    'B': '9c436477e63d9c7f5c8668dcd75fec9771f500a5e71b14a1f02dd0bcf8fe6c5b',
    'C14': 'ead833aee16aeb90571c48857d0d2f32de804a8ed6aa47fb28242d96f1aa97d3',
    'C15': '6a81410c3e046d87951316f28aa38ef4f43ed1268e06e052665b2b7c1e548f0b',
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
for arm in ('C14', 'C15'):
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
    'commonHelperSourceCommit': 'e659d58937ad4da2a4b18bb407890071a699b0d3',
    'candidateSourceCommits': {'C14': '55a4b0de46450a964808ba01bfcb6682c2ddcef6', 'C15': '5cff406be131b16e20c013ab79b89d988e9eb2cd'},
    'cases': ['ordinary', 'async-ordinary', 'async-alias2', 'async-custom'],
    'timingSchedule': ['B', 'C14', 'C15', 'B', 'C15', 'C14', 'B'],
    'allocationSchedule': ['B', 'C14', 'C15', 'B'],
    'allocationPolicy': {'DOTNET_GCHeapCount': '4', 'DOTNET_GCDynamicAdaptationMode': '0', 'mode': 0, 'noGcBudget': 536870912, 'calibrationBytes': 523240, 'blocks': 3, 'operationsPerBlock': 1000},
    'timingPolicy': {'runtimeOverrides': {}, 'mode': 1, 'warmSeconds': 3, 'warmupIterations': 3, 'actualIterations': 12, 'operationsPerInvocation': 1000, 'outlierRemoval': False, 'nativeMemoryDiagnoser': True},
    'childDeadlineSeconds': 60,
    'campaignDeadlineSeconds': 285,
    'cleanupMaximumSeconds': 10,
    'scope': 'Exact C12 actual production client helper, independent C14/C15 engines. Both mechanisms are exercised by async-alias2; ordinary, async-ordinary and async-custom are unaffected controls. Setup proves actual suspension; measured handler uses Task.Yield with natural await and full disposal. Larger cache fallback is correctness only. Native MemoryDiagnoser/default-GC elapsed first, separately calibrated process allocation afterward. No network or CPU claim.',
}
(OUTPUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
print(json.dumps({'manifestSha256': sha(OUTPUT / 'manifest.json'), 'runnerSha256': sha(HERE / 'run_bdn.py'), 'files': len(files), 'changes': changes}, indent=2))
