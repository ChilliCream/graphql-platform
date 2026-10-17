#!/usr/bin/env python3
"""Freeze one common executor harness and overlay only each arm's execution assembly."""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess

HERE = Path(__file__).resolve().parent
ROOT = Path('/workspaces/platform-1/perf/fusion-apollo-error-vars-20261017b-43a8dca2')
PREVIOUS = ROOT / 'frozen-process-scoped'
OUTPUT = ROOT / 'frozen-fixed-heaps'
BUILD = HERE / 'Micro/bin/Release/net10.0'
HARNESS = 'HotChocolate.Fusion.Execution.Benchmarks'
ENGINE = 'HotChocolate.Fusion.Execution'
EXPECTED = {
    'baseline': '9c436477e63d9c7f5c8668dcd75fec9771f500a5e71b14a1f02dd0bcf8fe6c5b',
    'candidate': '244ad610bfe4ab6152885955e22776124510acb344f8c783a3f3a17260878301',
}


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


if OUTPUT.exists():
    raise FileExistsError(OUTPUT)
for arm in EXPECTED:
    if sha(PREVIOUS / arm / f'{ENGINE}.dll') != EXPECTED[arm]:
        raise ValueError(f'Existing engine identity changed: {arm}')
shutil.copytree(PREVIOUS / 'baseline', OUTPUT / 'baseline')
for extension in ['.dll', '.pdb', '.deps.json', '.runtimeconfig.json']:
    shutil.copyfile(BUILD / (HARNESS + extension), OUTPUT / 'baseline' / (HARNESS + extension))
shutil.copytree(OUTPUT / 'baseline', OUTPUT / 'candidate')
for extension in ['.dll', '.pdb']:
    shutil.copyfile(PREVIOUS / 'candidate' / (ENGINE + extension), OUTPUT / 'candidate' / (ENGINE + extension))
files = {str(path.relative_to(OUTPUT)): sha(path)
         for path in sorted(OUTPUT.rglob('*')) if path.is_file()}
differences = sorted(str(path.relative_to(OUTPUT / 'baseline'))
                     for path in (OUTPUT / 'baseline').rglob('*')
                     if path.is_file() and sha(path) != sha(OUTPUT / 'candidate' / path.relative_to(OUTPUT / 'baseline')))
if differences != [ENGINE + '.dll', ENGINE + '.pdb']:
    raise ValueError(f'Unexpected arm differences: {differences}')
sources = [HERE / 'Micro/Program.cs', HERE / 'Micro/SchemaFixture.cs', HERE / 'Micro/Micro.csproj',
           HERE / 'run_micro.py', HERE / 'freeze_micro.py']
manifest = {
    'base': '43a8dca2bd479ddc411d832ed97e42d9fcc8bd85',
    'sourceCommit': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=HERE, text=True).strip(),
    'sourceHashes': {str(path.relative_to(HERE)): sha(path) for path in sources},
    'files': files, 'variantDifferences': differences,
    'allocationPolicy': {'DOTNET_GCHeapCount': '4', 'assertedHeapCount': 4,
                         'assertedGCDynamicAdaptationMode': 0, 'noGcBudgetBytes': 536870912},
    'timingPolicy': {'runtimeOverrides': {}, 'assertedGCDynamicAdaptationMode': 1},
    'warmupMillisecondsPerCase': 3000,
    'provenance': 'Common prior baseline dependencies plus one rebuilt harness; original baseline and candidate engine DLL/PDB overlays retained byte-for-byte.',
}
(OUTPUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
print(json.dumps({'path': str(OUTPUT), 'manifestSha256': sha(OUTPUT / 'manifest.json'),
                  'variantDifferences': differences}, indent=2))
