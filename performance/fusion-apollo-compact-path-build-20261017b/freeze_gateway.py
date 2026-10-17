#!/usr/bin/env python3
"""Freeze C9 by copying the published baseline gateway and replacing its measured engine only."""
import datetime
import hashlib
import json
from pathlib import Path
import shutil
import sys

baseline, overlay, destination, manifest_path = map(Path, sys.argv[1:])
expected = '91622b981639bd0304a050866de32f55371087b25b119398421d591b244e145a'
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
assert sha(overlay / 'HotChocolate.Fusion.Execution.dll') == expected
shutil.copytree(baseline, destination)
for name in ['HotChocolate.Fusion.Execution.dll', 'HotChocolate.Fusion.Execution.pdb']:
    shutil.copy2(overlay / name, destination / name)
original = {str(p.relative_to(baseline)): sha(p) for p in baseline.rglob('*') if p.is_file()}
candidate = {str(p.relative_to(destination)): sha(p) for p in destination.rglob('*') if p.is_file()}
assert original.keys() == candidate.keys()
differences = [name for name in original if original[name] != candidate[name]]
assert sorted(differences) == ['HotChocolate.Fusion.Execution.dll', 'HotChocolate.Fusion.Execution.pdb']
manifest = {'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
            'baseline_source':'43a8dca2bd479ddc411d832ed97e42d9fcc8bd85',
            'baseline_evidence':'ef9fc1159a5a7a980754ebd42b511030320482cd',
            'candidate_source':'d848b383a01bf7b01184ea8c7f3c42a73230017a',
            'baseline_directory':str(baseline), 'candidate_directory':str(destination),
            'differences':differences, 'baseline_files':original, 'candidate_files':candidate,
            'runtime_configuration':'unchanged baseline runtimeconfig: net10.0, 10.0.12, RollForward Disable, server GC',
            'fidelity':'copied exact baseline host/config/FAR; unchanged13node Apollo plan and fixed source fixtures; runtime loaded identity still requires campaign observation'}
manifest_path.parent.mkdir(parents=True,exist_ok=True)
manifest_path.write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps({'candidate_directory':str(destination),'differences':differences,'files':len(candidate)}))
