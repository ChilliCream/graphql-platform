#!/usr/bin/env python3
import datetime
import hashlib
import json
import os
from pathlib import Path
import signal
import subprocess
import time

ROOT = Path('/workspaces/platform-1/perf/fusion-apollo-source-stream-20261017b-43a8dca2')
FROZEN = ROOT / 'frozen-shared'
OUTPUT = ROOT / 'bdn-shared-campaign'
if set(os.sched_getaffinity(0)) != {0, 8}:
    raise RuntimeError('Runner must be pinned to CPUs 0,8.')
OUTPUT.mkdir(exist_ok=False)
records = []
deadline = time.monotonic() + 420


def utc():
    return datetime.datetime.now(datetime.UTC).isoformat()


def identity(pid):
    try:
        stat = Path(f'/proc/{pid}/stat').read_text()
        return {'pid': pid, 'startTicks': int(stat[stat.rfind(')') + 2:].split()[19])}
    except FileNotFoundError:
        return None


campaign = {'started': utc(), 'runner': identity(os.getpid()), 'status': 'starting',
            'runnerSha256': hashlib.sha256(Path(__file__).read_bytes()).hexdigest()}


def save():
    (OUTPUT / 'launches.json').write_text(json.dumps(records, indent=2) + '\n')
    (OUTPUT / 'campaign.json').write_text(json.dumps(campaign, indent=2) + '\n')


def interrupted(signum, frame):
    raise InterruptedError(f'Received signal {signum}.')


for sig in (signal.SIGTERM, signal.SIGINT):
    signal.signal(sig, interrupted)


def main():
    manifest = json.loads((FROZEN / 'manifest.json').read_text())
    campaign['manifestSha256'] = hashlib.sha256((FROZEN / 'manifest.json').read_bytes()).hexdigest()
    for relative, expected in manifest['files'].items():
        if hashlib.sha256((FROZEN / relative).read_bytes()).hexdigest() != expected:
            raise RuntimeError(f'Frozen artifact changed: {relative}')
    for arm, changes in manifest['differencesByArm'].items():
        if changes != ['HotChocolate.Fusion.Execution.dll', 'HotChocolate.Fusion.Execution.pdb']:
            raise RuntimeError(f'Unexpected variant differences: {arm}')
    environment = {key: value for key, value in os.environ.items()
                   if key.startswith(('DOTNET_', 'COMPlus_', 'CORECLR_'))}
    (OUTPUT / 'environment.json').write_text(json.dumps(environment, indent=2) + '\n')
    unexpected = {key: value for key, value in environment.items()
                  if key.startswith(('COMPlus_', 'CORECLR_', 'DOTNET_GC', 'DOTNET_Tiered', 'DOTNET_ReadyToRun'))}
    if unexpected:
        raise RuntimeError(f'Unexpected inherited runtime overrides: {sorted(unexpected)}')
    sequence = manifest['schedule']
    campaign.update({'schedule': sequence, 'status': 'running'})
    save()
    for index, (mode, arm) in enumerate(sequence):
        label = f'{index + 1:02}-{mode}-{arm}'
        artifact_dir = OUTPUT / label
        command = ['taskset', '-c', '2,3,10,11', 'dotnet',
                   str(FROZEN / arm / 'HotChocolate.Fusion.Execution.Benchmarks.dll'),
                   mode, str(artifact_dir)]
        launch_environment = dict(os.environ)
        if mode == 'allocate':
            launch_environment['DOTNET_GCHeapCount'] = '4'
        elif mode != 'benchmark':
            raise RuntimeError(f'Unexpected campaign mode: {mode}')
        started = time.monotonic()
        record = {'label': label, 'command': command, 'started': utc(), 'status': 'starting',
                  'runtimeEnvironment': {key: value for key, value in launch_environment.items()
                                         if key.startswith(('DOTNET_', 'COMPlus_', 'CORECLR_'))}}
        records.append(record)
        save()
        print(f"{record['started']} starting {label}", flush=True)
        process = None
        owned = None
        try:
            remaining = deadline - time.monotonic()
            if remaining <= 0:
                raise TimeoutError('Total campaign deadline reached.')
            with (OUTPUT / f'{label}.stdout').open('w') as stdout, (OUTPUT / f'{label}.stderr').open('w') as stderr:
                process = subprocess.Popen(command, stdout=stdout, stderr=stderr,
                                           env=launch_environment, start_new_session=True)
                owned = identity(process.pid)
                record.update({'identity': owned, 'processGroup': process.pid, 'status': 'running'})
                save()
                if owned is None:
                    raise RuntimeError('Child identity unavailable.')
                process.wait(timeout=min(75, remaining))
            record['exitCode'] = process.returncode
            if process.returncode:
                raise RuntimeError(f'{label} failed; no retry. Inspect preserved output.')
            report = json.loads((artifact_dir / 'runtime-identity.json').read_text())
            if report['runtime'] != '.NET 10.0.12' or not report['IsServerGC']:
                raise RuntimeError(f'{label}: runtime or GC policy mismatch.')
            expected_mode = 0 if mode == 'allocate' else 1
            if report['gcConfiguration']['GCDynamicAdaptationMode'] != expected_mode:
                raise RuntimeError(f'{label}: GC adaptation mismatch.')
            if mode == 'allocate' and report['gcConfiguration']['HeapCount'] != 4:
                raise RuntimeError(f'{label}: fixed heap count mismatch.')
            if report['executionDllSha256'] != manifest['files'][f'{arm}/HotChocolate.Fusion.Execution.dll']:
                raise RuntimeError(f'{label}: execution assembly identity mismatch.')
            if mode == 'allocate':
                results = json.loads((artifact_dir / 'allocation.json').read_text())
                if sorted(result['Shape'] for result in results) != sorted(manifest['cases']):
                    raise RuntimeError(f'{label}: incomplete allocation cases.')
                for result in results:
                    if len(result['blocks']) != 5:
                        raise RuntimeError(f'{label}: incomplete allocation blocks.')
                    for block in result['blocks']:
                        if (not block['valid'] or not block['synchronous'] or block['operations'] != 1000
                                or block['allocated'] < 0 or block['thread'] != block['endThread']
                                or block['calibrationBefore'] != 523240 or block['calibrationAfter'] != 523240
                                or block['gcBefore'] != block['gcAfter'] or block['endError'] is not None):
                            raise RuntimeError(f'{label}: invalid scoped allocation block.')
            else:
                reports = list(artifact_dir.rglob('*-full.json'))
                if len(reports) != 1:
                    raise RuntimeError(f'{label}: expected one complete BDN JSON report.')
                results = json.loads(reports[0].read_text())['Benchmarks']
                if len(results) != 3:
                    raise RuntimeError(f'{label}: incomplete BDN case set.')
                for result in results:
                    actual = [measurement for measurement in result['Measurements']
                              if measurement['IterationMode'] == 'Workload' and measurement['IterationStage'] == 'Actual']
                    if len(actual) != 12 or any(item['Operations'] != 1000 or item['Nanoseconds'] <= 0 for item in actual):
                        raise RuntimeError(f'{label}: incomplete or invalid BDN observations.')
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
            record.update({'finished': utc(), 'elapsedSeconds': time.monotonic() - started,
                           'cleanupActions': actions,
                           'ownedChildAlive': owned is not None and identity(owned['pid']) == owned,
                           'exitCode': process.returncode if process is not None else None})
            save()
        print(f"{record['finished']} completed {label}", flush=True)


try:
    main()
    campaign['status'] = 'completed'
except BaseException as error:
    campaign.update({'status': 'failed', 'exception': repr(error)})
    raise
finally:
    campaign['finished'] = utc()
    save()
