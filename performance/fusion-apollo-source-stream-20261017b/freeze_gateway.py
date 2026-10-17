#!/usr/bin/env python3
"""Copy the established gateway host and overlay one independently measured engine."""
import datetime
import hashlib
import json
from pathlib import Path
import shutil
import sys

arm = sys.argv[1]
ROOT = Path('/workspaces/platform-1/perf/fusion-apollo-source-stream-20261017b-43a8dca2')
BASELINE = Path('/workspaces/platform-1/perf/fusion-apollo-baseline-20261017b/frozen/gateway')
FROZEN = ROOT / 'frozen-shared-md-checked'
SOURCE = {
    'C7': '952cb180c25ad027a1b605ae625ac9d1c2a316f2',
    'C10': 'bee3ccac0532fd3a43fc58879641a7642b22c614',
}[arm]
EXPECTED = {
    'C7': 'c651030acc2ca721ee56a9bb0d85290f432cfeeb675d5b45f359bf33117be052',
    'C10': 'f6c74c8d89833eccca5fa5bfb5643c925e8ba34bc72a8f10e8982432072904b8',
}[arm]
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
assert sha(FROZEN / 'manifest.json') == '7296506f35edd27cd79bae5108f87bdfaadbbb8aa374ba3a8337338c4ba0d7dc'
assert sha(BASELINE / 'HotChocolate.Fusion.Execution.dll') == '9c436477e63d9c7f5c8668dcd75fec9771f500a5e71b14a1f02dd0bcf8fe6c5b'
assert sha(FROZEN / arm / 'HotChocolate.Fusion.Execution.dll') == EXPECTED
measured = json.loads((FROZEN / 'manifest.json').read_text())
destination = ROOT / 'gateway-overlays' / arm
shutil.copytree(BASELINE, destination)
for name in ['HotChocolate.Fusion.Execution.dll', 'HotChocolate.Fusion.Execution.pdb']:
    assert sha(FROZEN / arm / name) == measured['files'][f'{arm}/{name}']
    shutil.copy2(FROZEN / arm / name, destination / name)
original = {str(p.relative_to(BASELINE)): sha(p) for p in BASELINE.rglob('*') if p.is_file()}
candidate = {str(p.relative_to(destination)): sha(p) for p in destination.rglob('*') if p.is_file()}
assert len(original) == len(candidate) == 106
assert original.keys() == candidate.keys()
differences = sorted(name for name in original if original[name] != candidate[name])
assert differences == ['HotChocolate.Fusion.Execution.dll', 'HotChocolate.Fusion.Execution.pdb']
manifest = {
    'utc': datetime.datetime.now(datetime.UTC).isoformat(),
    'arm': arm,
    'baseline_source': '43a8dca2bd479ddc411d832ed97e42d9fcc8bd85',
    'baseline_evidence': 'ef9fc1159a5a7a980754ebd42b511030320482cd',
    'candidate_source': SOURCE,
    'micro_evidence': 'c5e7d9de844ac974dd20a1d784d457c2c866092c',
    'measured_manifest_sha256': sha(FROZEN / 'manifest.json'),
    'baseline_directory': str(BASELINE),
    'candidate_directory': str(destination),
    'differences': differences,
    'baseline_files': original,
    'candidate_files': candidate,
    'runtime_configuration': 'Unchanged baseline net10.0, runtime10.0.12, RollForward Disable, server GC.',
    'scope': 'Independent engine only, no host build or traffic. Loaded identity and runtime fidelity still require campaign observation.',
}
manifest_path = ROOT / 'gateway-overlays' / f'{arm}-manifest.json'
manifest_path.write_text(json.dumps(manifest, indent=2) + '\n')
print(json.dumps({'candidate_directory': str(destination), 'manifest': str(manifest_path),
                  'manifestSha256': sha(manifest_path), 'files': len(candidate),
                  'differences': differences, 'engineSha256': EXPECTED}))
