#!/usr/bin/env python3
import datetime
import hashlib
import json
import os
from pathlib import Path
import signal
import subprocess
import time

ROOT = Path('/workspaces/platform-1/perf/fusion-apollo-error-vars-20261017b-43a8dca2')
FROZEN = ROOT / 'frozen-process-scoped'
OUTPUT = ROOT / 'micro-campaign-process-scoped'
OUTPUT.mkdir(exist_ok=False)
deadline = time.monotonic() + 360
records = []


def utc():
    return datetime.datetime.now(datetime.UTC).isoformat()


def save():
    (OUTPUT / 'launches.json').write_text(json.dumps(records, indent=2) + '\n')


def identity(pid):
    try:
        stat = Path(f'/proc/{pid}/stat').read_text()
        return {'pid': pid, 'startTicks': int(stat[stat.rfind(')') + 2:].split()[19])}
    except FileNotFoundError:
        return None


def interrupted(signum, frame):
    raise InterruptedError(f'Received signal {signum}.')


for sig in (signal.SIGTERM, signal.SIGINT):
    signal.signal(sig, interrupted)

manifest = json.loads((FROZEN / 'manifest.json').read_text())
for relative, expected in manifest['files'].items():
    actual = hashlib.sha256((FROZEN / relative).read_bytes()).hexdigest()
    if actual != expected:
        raise RuntimeError(f'Frozen artifact changed: {relative}')
if manifest['variantDifferences'] != ['HotChocolate.Fusion.Execution.dll', 'HotChocolate.Fusion.Execution.pdb']:
    raise RuntimeError('Unexpected variant differences.')

sequence = [
    ('allocation', 'baseline'), ('allocation', 'candidate'), ('allocation', 'baseline'),
    ('timing', 'baseline'), ('timing', 'candidate'), ('timing', 'baseline'),
    ('timing', 'baseline'), ('timing', 'candidate'), ('timing', 'baseline'),
]
environment = {key: value for key, value in os.environ.items()
               if key.startswith(('DOTNET_', 'COMPlus_', 'CORECLR_'))}
(OUTPUT / 'environment.json').write_text(json.dumps(environment, indent=2) + '\n')
for index, (mode, variant) in enumerate(sequence):
    label = f'{index + 1:02}-{mode}-{variant}'
    dll = FROZEN / variant / 'HotChocolate.Fusion.Execution.Benchmarks.dll'
    command = ['taskset', '-c', '2,3,10,11', 'dotnet', str(dll), mode]
    start = time.monotonic()
    record = {'label': label, 'command': command, 'started': utc(), 'status': 'starting'}
    records.append(record)
    save()
    print(f"{record['started']} starting {label}", flush=True)
    process = None
    owned = None
    try:
        remaining = deadline - time.monotonic()
        if remaining <= 0:
            raise TimeoutError('Total campaign deadline reached.')
        with (OUTPUT / f'{label}.json').open('w') as stdout, (OUTPUT / f'{label}.stderr').open('w') as stderr:
            process = subprocess.Popen(command, stdout=stdout, stderr=stderr, start_new_session=True)
            owned = identity(process.pid)
            record.update({'identity': owned, 'processGroup': process.pid, 'status': 'running'})
            save()
            if owned is None:
                raise RuntimeError('Child identity unavailable.')
            process.wait(timeout=min(75, remaining))
        record['exitCode'] = process.returncode
        if process.returncode:
            raise RuntimeError(f'{label} failed; no retry. Inspect preserved stderr.')
        report = json.loads((OUTPUT / f'{label}.json').read_text())
        if report['runtime'] != '.NET 10.0.12' or not report['IsServerGC']:
            raise RuntimeError(f'{label}: runtime or GC policy mismatch.')
        if len(report['rows']) != 10:
            raise RuntimeError(f'{label}: incomplete case set.')
        if report['executionDllSha256'] != manifest['files'][f'{variant}/HotChocolate.Fusion.Execution.dll']:
            raise RuntimeError(f'{label}: execution assembly identity mismatch.')
        for row in report['rows']:
            if mode == 'allocation' and any(
                    not b['noCollections'] or not b['noGc']
                    or b['calibrationBefore']['ThreadBytes'] != 523240 or b['calibrationAfter']['ThreadBytes'] != 523240
                    or b['calibrationBefore']['TotalBytes'] != 523240 or b['calibrationAfter']['TotalBytes'] != 523240
                    or b['completed'] != b['iterations']
                    or b['endError'] is not None for b in row['blocks']):
                raise RuntimeError(f'{label}: invalid scoped allocation observation.')
        record['status'] = 'completed'
    except BaseException as exception:
        record.update({'status': 'failed', 'exception': repr(exception)})
        raise
    finally:
        actions = []
        if process is not None and owned is not None and process.poll() is None and identity(process.pid) == owned:
            try:
                os.killpg(process.pid, signal.SIGTERM)
                actions.append('SIGTERM verified owned process group')
            except ProcessLookupError:
                actions.append('Owned process exited before SIGTERM')
            try:
                process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                if identity(process.pid) == owned:
                    try:
                        os.killpg(process.pid, signal.SIGKILL)
                        actions.append('SIGKILL verified owned process group')
                    except ProcessLookupError:
                        actions.append('Owned process exited before SIGKILL')
                process.wait(timeout=5)
        record.update({'finished': utc(), 'elapsedSeconds': time.monotonic() - start,
                       'cleanupActions': actions, 'ownedChildAlive': owned is not None and identity(owned['pid']) == owned,
                       'exitCode': process.returncode if process is not None else None})
        save()
    print(f"{record['finished']} completed {label}", flush=True)
