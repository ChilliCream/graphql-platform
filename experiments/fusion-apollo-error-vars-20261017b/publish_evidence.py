#!/usr/bin/env python3
"""Copy only owned logs, manifests, verification data and diagnostics for publication."""
import hashlib
import json
from pathlib import Path
import shutil

SOURCE = Path('/workspaces/platform-1/perf/fusion-apollo-error-vars-20261017b-43a8dca2')
DESTINATION = Path(__file__).resolve().parent / 'results'
DESTINATION.mkdir(exist_ok=True)
selected = [path for path in SOURCE.iterdir()
            if path.is_file() and path.suffix in {'.log', '.json', '.stderr', '.txt'}]
for directory in ['rejected-preexecution-snapshots', 'micro-campaign',
                  'micro-campaign-process-scoped', 'micro-campaign-fixed-heaps',
                  'gc-reason-diagnostic']:
    selected.extend(path for path in (SOURCE / directory).rglob('*')
                    if path.is_file() and path.suffix in {'.json', '.jsonl', '.log', '.stderr', '.md', '.nettrace'})
for directory in SOURCE.glob('frozen*'):
    selected.extend(path for path in directory.iterdir()
                    if path.is_file() and path.name in {'manifest.json', 'SUPERSEDED.txt'})
manifest = {}
for source in sorted(selected):
    relative = source.relative_to(SOURCE)
    destination = DESTINATION / relative
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(source, destination)
    manifest[str(relative)] = {'bytes': source.stat().st_size,
                               'sha256': hashlib.sha256(source.read_bytes()).hexdigest()}
(DESTINATION / 'artifact-manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
print(f'Copied {len(manifest)} explicitly selected owned artifacts.')
