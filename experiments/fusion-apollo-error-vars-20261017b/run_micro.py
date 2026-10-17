#!/usr/bin/env python3
import datetime
import hashlib
import json
import os
from pathlib import Path
import subprocess
import time

ROOT = Path('/workspaces/platform-1/perf/fusion-apollo-error-vars-20261017b-43a8dca2')
FROZEN = ROOT / 'frozen'
OUTPUT = ROOT / 'micro-campaign'
OUTPUT.mkdir(exist_ok=False)
manifest = json.loads((FROZEN / 'manifest.json').read_text())
for relative, expected in manifest['files'].items():
    actual = hashlib.sha256((FROZEN / relative).read_bytes()).hexdigest()
    if actual != expected:
        raise RuntimeError(f'Frozen artifact changed: {relative}')

sequence = [
    ('allocation', 'baseline'), ('allocation', 'candidate'), ('allocation', 'baseline'),
    ('timing', 'baseline'), ('timing', 'candidate'), ('timing', 'baseline'),
    ('timing', 'baseline'), ('timing', 'candidate'), ('timing', 'baseline'),
]
records = []
environment = {key: value for key, value in os.environ.items()
               if key.startswith(('DOTNET_', 'COMPlus_', 'CORECLR_'))}
(OUTPUT / 'environment.json').write_text(json.dumps(environment, indent=2) + '\n')
for index, (mode, variant) in enumerate(sequence):
    label = f'{index + 1:02}-{mode}-{variant}'
    dll = FROZEN / variant / 'HotChocolate.Fusion.Execution.Benchmarks.dll'
    command = ['taskset', '-c', '2,3,10,11', 'dotnet', str(dll), mode]
    started = datetime.datetime.now(datetime.UTC).isoformat()
    start = time.monotonic()
    print(f'{started} starting {label}', flush=True)
    with (OUTPUT / f'{label}.json').open('w') as stdout, (OUTPUT / f'{label}.stderr').open('w') as stderr:
        result = subprocess.run(command, stdout=stdout, stderr=stderr, timeout=75, check=False)
    record = {'label': label, 'command': command, 'started': started,
              'finished': datetime.datetime.now(datetime.UTC).isoformat(),
              'elapsedSeconds': time.monotonic() - start, 'exitCode': result.returncode}
    records.append(record)
    (OUTPUT / 'launches.json').write_text(json.dumps(records, indent=2) + '\n')
    if result.returncode:
        raise RuntimeError(f'{label} failed; no retry. Inspect preserved stderr.')
    report = json.loads((OUTPUT / f'{label}.json').read_text())
    if len(report['rows']) != 10:
        raise RuntimeError(f'{label}: incomplete case set.')
    for row in report['rows']:
        if not row['sync'] or row['OtherThreadObserved']:
            raise RuntimeError(f'{label}: asynchronous or off-thread execution.')
        if mode == 'allocation' and any(not b['noCollections'] or not b['noGc'] for b in row['blocks']):
            raise RuntimeError(f'{label}: invalid scoped allocation observation.')
    print(f'{record["finished"]} completed {label}', flush=True)
