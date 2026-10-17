#!/usr/bin/env python3
"""One GC-reason probe. Profiled allocation observations are not performance results."""
import datetime
import hashlib
import json
import os
from pathlib import Path
import selectors
import signal
import subprocess
import time

ROOT = Path('/workspaces/platform-1/perf/fusion-apollo-error-vars-20261017b-43a8dca2')
FROZEN = ROOT / 'frozen-process-scoped'
OUTPUT = ROOT / 'gc-reason-diagnostic'
TRACE = Path('/workspaces/platform-1/perf/fusion-apollo-20261017-43a8dca2/environment-history/tools/dotnet-trace')
DLL = FROZEN / 'candidate/HotChocolate.Fusion.Execution.Benchmarks.dll'
OUTPUT.mkdir(exist_ok=False)


def utc():
    return datetime.datetime.now(datetime.UTC).isoformat()


def identity(pid):
    try:
        stat = Path(f'/proc/{pid}/stat').read_text()
        fields = stat[stat.rfind(')') + 2:].split()
        return {'pid': pid, 'startTicks': int(fields[19]), 'processGroup': int(fields[2])}
    except (FileNotFoundError, ProcessLookupError):
        return None


def interrupted(signum, frame):
    raise InterruptedError(f'Received signal {signum}.')


for sig in (signal.SIGTERM, signal.SIGINT):
    signal.signal(sig, interrupted)

manifest = json.loads((FROZEN / 'manifest.json').read_text())
for relative, expected in manifest['files'].items():
    if hashlib.sha256((FROZEN / relative).read_bytes()).hexdigest() != expected:
        raise RuntimeError(f'Frozen artifact changed: {relative}')

command = ['taskset', '-c', '0,8', str(TRACE), 'collect',
           '--providers', 'Microsoft-Windows-DotNETRuntime:0x1:4',
           '--buffersize', '64', '--duration', '00:00:00:30', '--show-child-io',
           '--output', str(OUTPUT / 'gc-reason.nettrace'), '--',
           'taskset', '-c', '2,3,10,11', 'dotnet', str(DLL), 'allocation']
record = {'started': utc(), 'command': command, 'status': 'starting',
          'interpretation': 'Diagnostic only. No profiled allocation or timing result is accepted.',
          'outputTimestampMeaning': 'UTC receipt time of each complete merged tool/child output line.',
          'manifestSha256': hashlib.sha256((FROZEN / 'manifest.json').read_bytes()).hexdigest(),
          'environment': {key: value for key, value in os.environ.items()
                          if key.startswith(('DOTNET_', 'COMPlus_', 'CORECLR_'))}}
process = None
owned = None
target = None
start = time.monotonic()


def save():
    (OUTPUT / 'launch.json').write_text(json.dumps(record, indent=2) + '\n')


def group_is_owned():
    return any(item is not None and identity(item['pid']) == item for item in (owned, target))


save()
try:
    process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                               start_new_session=True)
    owned = identity(process.pid)
    record.update({'toolIdentity': owned, 'status': 'running'})
    save()
    if owned is None:
        raise RuntimeError('Tool process identity unavailable.')
    selector = selectors.DefaultSelector()
    os.set_blocking(process.stdout.fileno(), False)
    selector.register(process.stdout, selectors.EVENT_READ)
    pending = b''
    next_scan = 0.0
    with (OUTPUT / 'output.log').open('wb') as raw, (OUTPUT / 'output-timestamps.jsonl').open('w') as stamped:
        while process.poll() is None or selector.get_map():
            now = time.monotonic()
            if now - start > 45:
                raise TimeoutError('Single diagnostic reached the 45-second deadline.')
            if target is None and now >= next_scan:
                next_scan = now + 0.05
                for entry in Path('/proc').iterdir():
                    if not entry.name.isdigit() or int(entry.name) == process.pid:
                        continue
                    candidate = identity(int(entry.name))
                    if candidate is None or candidate['processGroup'] != process.pid:
                        continue
                    try:
                        arguments = (entry / 'cmdline').read_bytes().split(b'\0')
                    except (FileNotFoundError, ProcessLookupError):
                        continue
                    if str(DLL).encode() in arguments:
                        target = candidate
                        record.update({'targetIdentity': target, 'targetObservedUtc': utc()})
                        save()
                        break
            for key, events in selector.select(timeout=0.05):
                chunk = os.read(key.fileobj.fileno(), 65536)
                if not chunk:
                    selector.unregister(key.fileobj)
                    continue
                raw.write(chunk)
                raw.flush()
                pending += chunk
                while b'\n' in pending:
                    line, pending = pending.split(b'\n', 1)
                    stamped.write(json.dumps({'observedUtc': utc(), 'line': line.decode(errors='replace')}) + '\n')
                    stamped.flush()
        if pending:
            stamped.write(json.dumps({'observedUtc': utc(), 'partialLine': pending.decode(errors='replace')}) + '\n')
    if target is None:
        raise RuntimeError('Target process identity was not observed.')
    record.update({'status': 'captured', 'exitCode': process.returncode})
except BaseException as exception:
    record.update({'status': 'failed', 'exception': repr(exception)})
    raise
finally:
    actions = []
    if process is not None and group_is_owned():
        try:
            os.killpg(process.pid, signal.SIGTERM)
            actions.append('SIGTERM verified owned process group')
        except ProcessLookupError:
            actions.append('Owned process group exited before SIGTERM')
        end = time.monotonic() + 5
        while group_is_owned() and time.monotonic() < end:
            if process.poll() is None:
                try:
                    process.wait(timeout=0.05)
                except subprocess.TimeoutExpired:
                    pass
            else:
                time.sleep(0.05)
        if group_is_owned():
            os.killpg(process.pid, signal.SIGKILL)
            actions.append('SIGKILL verified owned process group')
    if process is not None:
        process.wait(timeout=5)
    record.update({'finished': utc(), 'elapsedSeconds': time.monotonic() - start,
                   'cleanupActions': actions, 'ownedProcessAlive': group_is_owned(),
                   'exitCode': process.returncode if process is not None else None})
    save()
    print(json.dumps(record, indent=2), flush=True)
