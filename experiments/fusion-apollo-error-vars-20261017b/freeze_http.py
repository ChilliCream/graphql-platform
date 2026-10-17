#!/usr/bin/env python3
"""Overlay the measured engine on the established frozen benchmark host."""
import hashlib
import json
from pathlib import Path
import shutil

ROOT = Path('/workspaces/platform-1/perf/fusion-apollo-error-vars-20261017b-43a8dca2')
BASELINE = Path('/workspaces/platform-1/perf/fusion-apollo-baseline-20261017b/frozen/gateway')
CANDIDATE = ROOT / 'frozen-fixed-heaps/candidate'
OUTPUT = ROOT / 'frozen-http/gateway'
ENGINE = 'HotChocolate.Fusion.Execution'


def hashes(directory):
    return {str(path.relative_to(directory)): hashlib.sha256(path.read_bytes()).hexdigest()
            for path in sorted(directory.rglob('*')) if path.is_file()}


control = hashes(BASELINE)
if control[ENGINE + '.dll'] != '9c436477e63d9c7f5c8668dcd75fec9771f500a5e71b14a1f02dd0bcf8fe6c5b':
    raise ValueError('Established baseline engine identity changed.')
if hashes(CANDIDATE)[ENGINE + '.dll'] != '244ad610bfe4ab6152885955e22776124510acb344f8c783a3f3a17260878301':
    raise ValueError('Measured candidate engine identity changed.')
shutil.copytree(BASELINE, OUTPUT)
for suffix in ['.dll', '.pdb']:
    shutil.copyfile(CANDIDATE / (ENGINE + suffix), OUTPUT / (ENGINE + suffix))
candidate = hashes(OUTPUT)
assert candidate.keys() == control.keys()
changed = [name for name in candidate if candidate[name] != control[name]]
assert changed == [ENGINE + '.dll', ENGINE + '.pdb']
manifest = {'baselineEvidenceCommit': 'ef9fc1159a5a7a980754ebd42b511030320482cd',
            'baselineGateway': str(BASELINE), 'candidateGateway': str(OUTPUT),
            'baselineFiles': control, 'candidateFiles': candidate, 'changedFiles': changed,
            'policy': 'Host, FAR, runtime, security, logging and all dependencies are baseline-identical. Default GC; allocation-only micro environment is not applied.'}
(OUTPUT.parent / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
print(json.dumps({'gateway': str(OUTPUT), 'fileCount': len(candidate), 'changedFiles': changed,
                  'dll': candidate[ENGINE + '.dll'], 'pdb': candidate[ENGINE + '.pdb']}, indent=2))
