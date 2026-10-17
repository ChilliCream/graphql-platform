#!/usr/bin/env python3
from pathlib import Path
import hashlib
import json
import shutil
HERE = Path(__file__).resolve().parent
ROOT = Path('/workspaces/platform-1/perf/fusion-apollo-path-lookup-20261017b')
OUTPUT = ROOT / 'frozen-bdn'
COMMON = Path('/workspaces/platform-1/perf/fusion-apollo-error-vars-20261017b-43a8dca2/frozen-bdn/B')
ENGINE = 'HotChocolate.Fusion.Execution'
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()
if OUTPUT.exists():
    raise FileExistsError(OUTPUT)
for arm in ['B', 'C5']:
    shutil.copytree(COMMON, OUTPUT / arm)
for suffix in ['.dll', '.pdb']:
    shutil.copyfile(ROOT / 'frozen' / (ENGINE + suffix), OUTPUT / 'C5' / (ENGINE + suffix))
assert sha(OUTPUT / 'B' / (ENGINE + '.dll')) == '9c436477e63d9c7f5c8668dcd75fec9771f500a5e71b14a1f02dd0bcf8fe6c5b'
assert sha(OUTPUT / 'C5' / (ENGINE + '.dll')) == 'eb3a59ad4d4d0ba5098b646dcdf973ba0a6d743b11f640738e2299babfb01214'
files = {str(p.relative_to(OUTPUT)): sha(p) for p in sorted(OUTPUT.rglob('*')) if p.is_file()}
differences = sorted(str(p.relative_to(OUTPUT / 'B')) for p in (OUTPUT / 'B').rglob('*')
                     if p.is_file() and sha(p) != sha(OUTPUT / 'C5' / p.relative_to(OUTPUT / 'B')))
assert differences == [ENGINE + '.dll', ENGINE + '.pdb']
manifest = {'base': '43a8dca2bd479ddc411d832ed97e42d9fcc8bd85',
            'candidateSourceCommit': 'aaf7e69ddb556a74ca43c20d2ff2342113954ee5',
            'commonHostCommit': 'a59c7ac49408e02bd60e53704ae1f7d37a5af6db',
            'sourceHashes': {p.name: sha(p) for p in [HERE / 'run.py', HERE / 'freeze.py']},
            'files': files, 'variantDifferences': differences,
            'schedule': ['B', 'C5', 'B', 'C5', 'B'],
            'cases': ['ordinary', 'batch9', 'duplicate96-to1'],
            'runtimePolicy': 'Default .NET 10.0.12 server GC, adaptation mode1, no overrides',
            'metric': 'BDN elapsed, no allocation claim and no process CPU measurement',
            'prewarmSecondsPerCase': 3, 'warmupIterations': 3, 'measurementIterations': 12,
            'operationsPerInvocation': 1000, 'outlierRemoval': False,
            'childDeadlineSeconds': 60, 'campaignDeadlineSeconds': 165, 'cleanupMaximumSeconds': 10}
(OUTPUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
print(json.dumps({'manifestSha256': sha(OUTPUT / 'manifest.json'), 'runnerSha256': sha(HERE / 'run.py'), 'differences': differences}, indent=2))
