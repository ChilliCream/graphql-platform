"""Prepare a frozen, explicit HTTP schedule or execute an approved schedule once.

Execution requires the root's exclusive measurement grant and external flock -x -o.
Preparation hashes files but starts no processes or HTTP traffic.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import signal
import subprocess
import time

HERE = Path(__file__).resolve().parent
ROOT = Path('/workspaces/platform-1')
BASE = ROOT / 'perf/fusion-apollo-baseline-20261017b'
BASELINE_COMMIT = 'ef9fc1159a5a7a980754ebd42b511030320482cd'
C3_GATEWAY = ROOT / 'perf/fusion-apollo-duplicate-paths-20261017b/frozen/gateway'
C3_DLL = '359a421c3387637ab022c658266252616e0ab58e262c72ad57a47c6ca6ae50cf'
C3_PDB = 'bf75447d5c73a2d293240a96b51b82dec989276d74a9765963763f699c470bee'
ENGINE = 'HotChocolate.Fusion.Execution'
ORIGINAL_K6 = 'b967285db74541fd9b29ab68522b6f5902de4caf7e43946dab183205bc88ea6e'
ORDERS = {
    'C3': ['B', 'C3', 'B', 'C3', 'B'],
    'C3,C2': ['B', 'C3', 'C2', 'B', 'C2', 'C3', 'B'],
}
SOURCES = ['Accounts', 'Inventory', 'Products', 'Reviews']
WORK_SECONDS = 48 * 60
CLEANUP_RESERVE_SECONDS = 120
WORK_DEADLINE = None
ACTIVE_SESSION = None
RETAINED = HERE.parent / 'results/http-initial-baseline'


def continuation_schedule():
    order = ORDERS['C3,C2']
    ordinary = [{'family': 'ordinary', 'delay_ms': 0, 'index': i, 'arm': arm,
                 'name': f'ordinary-{i:02d}-{arm}', 'cases': ['ordinary-fixed']}
                for i, arm in enumerate(order)]
    apollo = [{'family': 'apollo', 'delay_ms': delay, 'index': i, 'arm': arm,
               'name': f'delay{delay}-{i:02d}-{arm}',
               'cases': ['TestQuery', 'entities9'] if delay == 0 else ['TestQuery', 'burst']}
              for delay in [0, 4] for i, arm in enumerate(order) if delay != 0 or i != 0]
    return [item for item in apollo if item['delay_ms'] == 0] + ordinary + [item for item in apollo if item['delay_ms'] == 4]


def load_module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


baseline = load_module('published_baseline_campaign', HERE / 'vendor/campaign.py')
baseline.HERE = HERE / 'vendor'


def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(chunk)
    return digest.hexdigest()


def hashes(directory):
    return {str(p.relative_to(directory)): sha(p) for p in sorted(directory.rglob('*'))
            if p.is_file() and '__pycache__' not in p.parts and p.suffix != '.pyc'}


def dump(path, value):
    baseline.dump(path, value)


def validate_candidate(label, gateway, control, expected):
    found = hashes(gateway)
    if found.keys() != control.keys():
        raise ValueError(f'{label} file set differs from the frozen baseline')
    changed = {name for name in found if found[name] != control[name]}
    allowed = {ENGINE + '.dll', ENGINE + '.pdb'}
    if changed != allowed:
        raise ValueError(f'{label} changes other than the execution assembly/PDB: {changed}')
    for suffix, value in expected.items():
        if found[ENGINE + suffix] != value:
            raise ValueError(f'{label} {suffix} identity mismatch')
    return found


def prepare(args):
    args.plan = args.plan.resolve()
    if args.plan.is_relative_to(HERE):
        raise ValueError('Write the plan outside the hashed runner source directory')
    if args.plan.exists():
        raise FileExistsError(args.plan)
    if args.survivors != 'C3,C2':
        raise ValueError('The approved continuation has both independently validated survivors')
    if args.survivors == 'C3,C2' and not all((args.c2_gateway, args.c2_dll, args.c2_pdb)):
        raise ValueError('C2 requires an independently accepted gateway path and both frozen hashes')
    if args.survivors == 'C3' and any((args.c2_gateway, args.c2_dll, args.c2_pdb)):
        raise ValueError('Do not provide C2 artifacts for a C3-only schedule')
    args.plan.parent.mkdir(parents=True, exist_ok=True)
    control = hashes(BASE / 'frozen/gateway')
    arms = {'B': {'gateway': str(BASE / 'frozen/gateway'), 'hashes': control}}
    arms['C3'] = {'gateway': str(C3_GATEWAY), 'hashes': validate_candidate(
        'C3', C3_GATEWAY, control, {'.dll': C3_DLL, '.pdb': C3_PDB})}
    if args.survivors == 'C3,C2':
        arms['C2'] = {'gateway': str(args.c2_gateway.resolve()), 'hashes': validate_candidate(
            'C2', args.c2_gateway, control, {'.dll': args.c2_dll, '.pdb': args.c2_pdb})}
    if sha(HERE / 'vendor/fixture/k6/k6.js') != ORIGINAL_K6:
        raise ValueError('Original benchmark driver changed')
    if not control or not all(hashes(BASE / 'frozen' / source) for source in SOURCES):
        raise ValueError('Missing frozen fixture')
    order = ORDERS[args.survivors]
    plan = {
        'created': baseline.stamp(), 'baseline_commit': BASELINE_COMMIT,
        'source_commit': '43a8dca2bd479ddc411d832ed97e42d9fcc8bd85',
        'survivors': args.survivors, 'order_per_delay': order, 'delays_ms': [0, 4],
        'arms': arms, 'sources': {n: {'path': str(BASE / 'frozen' / n),
                                    'hashes': hashes(BASE / 'frozen' / n)} for n in SOURCES},
        'runner_sources': hashes(HERE),
        'tools': {str(baseline.K6): sha(baseline.K6),
                  str(baseline.TOOLS / 'dotnet-counters'): sha(baseline.TOOLS / 'dotnet-counters')},
        'duration_seconds': {'warmup': 30, 'TestQuery': 30, 'entities9': 20,
                             'ordinary-fixed': 20, 'burst': 60, 'idle': 20},
        'ordinary': {'rate': 5000, 'time_unit': '1s', 'preallocated_vus': 200,
                     'max_vus': 200, 'nominal_iterations': 100000, 'setup_requests': 100,
                     'allowed_dropped': 0,
                     'count_contract': 'Exact native/summary measured requests=iterations=successes; total HTTP adds100setup; actual scheduled count/time/rate reported, no integer tolerance'},
        'schedules': continuation_schedule(),
        'retained': {'path': str(RETAINED), 'hashes': hashes(RETAINED),
                     'evidence_commit': 'ad60368f0e309abc60d5c6e6fcc2cbf24946db38',
                     'session': 'delay0-00-B',
                     'arm': {'family': 'apollo', 'delay_ms': 0, 'index': 0, 'arm': 'B',
                             'name': 'retained-delay0-00-B', 'cases': ['TestQuery', 'entities9']}},
        'execution_authorized': False,
        'session_bound': {'work_seconds': WORK_SECONDS,
                          'cleanup_reserve_seconds': CLEANUP_RESERVE_SECONDS,
                          'total_seconds': WORK_SECONDS + CLEANUP_RESERVE_SECONDS},
    }
    dump(args.plan, plan)
    print(f'Prepared {len(plan["schedules"])} launches; no HTTP executed: {args.plan}')


def verify(plan):
    if plan['session_bound'] != {'work_seconds': WORK_SECONDS,
                                 'cleanup_reserve_seconds': CLEANUP_RESERVE_SECONDS,
                                 'total_seconds': WORK_SECONDS + CLEANUP_RESERVE_SECONDS}:
        raise ValueError('Whole-session time bound changed')
    if plan['order_per_delay'] != ORDERS[plan['survivors']] or plan['delays_ms'] != [0, 4]:
        raise ValueError('Schedule differs from the reviewed bounded orders')
    expected = continuation_schedule()
    if plan['schedules'] != expected:
        raise ValueError('Launch schedule was altered')
    if hashes(HERE) != plan['runner_sources']:
        raise ValueError('Runner, oracle, workload or analysis source changed after freeze')
    if hashes(Path(plan['retained']['path'])) != plan['retained']['hashes']:
        raise ValueError('Retained valid first baseline observations changed')
    if plan['retained']['arm'] != {'family': 'apollo', 'delay_ms': 0, 'index': 0, 'arm': 'B',
                                  'name': 'retained-delay0-00-B', 'cases': ['TestQuery', 'entities9']}:
        raise ValueError('Retained control selection changed')
    if plan['ordinary'] != {'rate': 5000, 'time_unit': '1s', 'preallocated_vus': 200,
                           'max_vus': 200, 'nominal_iterations': 100000, 'setup_requests': 100,
                           'allowed_dropped': 0,
                           'count_contract': 'Exact native/summary measured requests=iterations=successes; total HTTP adds100setup; actual scheduled count/time/rate reported, no integer tolerance'}:
        raise ValueError('Corrected ordinary configuration changed')
    for arm, definition in plan['arms'].items():
        if hashes(Path(definition['gateway'])) != definition['hashes']:
            raise ValueError(f'Frozen gateway changed: {arm}')
    for name, definition in plan['sources'].items():
        if hashes(Path(definition['path'])) != definition['hashes']:
            raise ValueError(f'Frozen source changed: {name}')
    for name, identity in plan['tools'].items():
        if sha(Path(name)) != identity:
            raise ValueError(f'Tool changed: {name}')
    # The original host records these settings in its runtime configuration.
    config = json.loads((Path(plan['arms']['B']['gateway']) / 'eShop.Gateway.runtimeconfig.json').read_text())
    opts = config['runtimeOptions']
    if opts['configProperties'].get('System.GC.Server') is not True:
        raise ValueError('Server GC configuration changed')
    if opts.get('rollForward') != 'Disable':
        raise ValueError('Runtime roll-forward policy changed')


class Session(baseline.Session):
    def __init__(self, out, delay, capture=False):
        global ACTIVE_SESSION
        super().__init__(out, delay, capture)
        ACTIVE_SESSION = self

    def close(self):
        global ACTIVE_SESSION
        # Cleanup has at most seven live owned children, each bounded to 8+3 seconds.
        # Defer the workload alarm while completing that cleanup, then restore it.
        armed = signal.getitimer(signal.ITIMER_REAL)[0] > 0
        signal.setitimer(signal.ITIMER_REAL, 0)
        try:
            super().close()
            ACTIVE_SESSION = None
        finally:
            if armed:
                signal.setitimer(signal.ITIMER_REAL, max(.001, WORK_DEADLINE - time.monotonic()))

    def require_live(self):
        for process, identity, _ in self.owned:
            if identity['name'] in [*SOURCES, 'gateway', 'counters'] and process.poll() is not None:
                raise RuntimeError(f'Required process exited: {identity["name"]}, {process.returncode}')

    def marker(self, name, **extra):
        row = {'name': name, 'utc': baseline.stamp(), 'epoch': time.time(),
               'monotonic': time.monotonic(), **extra}
        with (self.out / 'boundaries.jsonl').open('a') as stream:
            stream.write(json.dumps(row) + '\n')
        print(row['utc'], self.out.name, name, flush=True)

    def start(self):
        super().start()
        maps = (self.out / 'gateway-maps.txt').read_text()
        if '/Microsoft.NETCore.App/10.0.12/' not in maps:
            raise ValueError('Gateway did not load the pinned runtime 10.0.12')
        if ENGINE + '.dll' not in maps:
            raise ValueError('Execution assembly missing from observed module maps')
        self.marker('ready')
        return self

    def preflight(self):
        self.marker('preflight-start')
        for name in ['TestQuery', 'entities9', 'ordinary']:
            payload = json.loads((HERE / f'vendor/workloads/{name}.json').read_text())
            expected = json.loads((HERE / f'vendor/oracles/{name}-oracle.json').read_text())
            response = baseline.post(5220, payload, {'Authorization': f'Bearer preflight-{self.out.name}-{name}'})
            dump(self.out / (name + '-preflight.json'), response)
            if response != expected:
                raise ValueError(f'Complete-data preflight failed for {name}')
        self.marker('preflight-end')

    def k6(self, name, workload='TestQuery', duration=30, mode='constant', vus=50, raw=True):
        self.require_live()
        out = self.out / name
        out.mkdir()
        args = [baseline.K6, 'run', '--quiet', '--address', '127.0.0.1:0']
        if raw:
            args += ['--out', f'json={out}/samples.json.gz']
        settings = {'MODE': mode, 'BENCH_OVER_TIME': f'{duration}s', 'BENCH_VUS': vus,
                    'GATEWAY_ENDPOINT': baseline.ENDPOINT, 'SUMMARY_PATH': out,
                    'WORKLOAD_PATH': HERE / f'vendor/workloads/{workload}.json',
                    'ORACLE_PATH': HERE / f'vendor/oracles/{workload}-oracle.json'}
        for key, value in settings.items():
            args += ['-e', f'{key}={value}']
        if name == 'ordinary-fixed':
            script = HERE / 'ordinary-fixed.js'
        elif workload == 'TestQuery':
            script = HERE / 'vendor/fixture/k6/k6.js'
        else:
            script = HERE / 'vendor/focused.js'
        args += [script]
        dump(out / 'command.json', {'argv': list(map(str, args)), 'start': baseline.stamp(),
                                   'workload': workload, 'duration': duration, 'mode': mode,
                                   'vus': vus, 'timeout_seconds': duration + 120,
                                   'fixed_arrival_rate': 5000 if name == 'ordinary-fixed' else None})
        self.marker(name + '-before-setup')
        process = self.launch(name, args, HERE / 'vendor', {}, '1,9')
        try:
            rc = process.wait(timeout=duration + 120)
        except subprocess.TimeoutExpired:
            dump(out / 'completion.json', {'end': baseline.stamp(), 'returncode': None,
                                           'timeout': True})
            # Session.close verifies ownership and terminates all owned children.
            raise
        with self.lock:
            self.live.pop(name, None)
        dump(out / 'completion.json', {'end': baseline.stamp(), 'returncode': rc})
        self.marker(name + '-generator-complete')
        self.require_live()
        if rc != 0:
            raise RuntimeError(f'k6 {name} exited {rc}')
        summary = json.loads((out / 'k6_summary.json').read_text())['metrics']
        if summary.get('success_rate', {}).get('values', {}).get('rate') != 1:
            raise ValueError(f'{name} has unsuccessful responses')
        if summary.get('http_req_failed', {}).get('values', {}).get('rate') != 0:
            raise ValueError(f'{name} has failed HTTP responses, including setup')
        dropped = summary.get('dropped_iterations', {}).get('values', {}).get('count', 0)
        if dropped != 0:
            raise ValueError(f'{name} dropped {dropped} iterations')
        if name == 'ordinary-fixed':
            measured = summary['iterations']['values']['count']
            total = summary['http_reqs']['values']['count']
            success_values = summary['success_rate']['values']
            if measured <= 0 or total != measured + 100 or success_values['passes'] != measured or success_values['fails'] != 0:
                raise ValueError(f'Fixed-rate summary accounting failed: {measured}, {total}, {success_values}')
            dump(out / 'offered-load-accounting.json', {
                'configured_rate_per_second': 5000, 'configured_duration_seconds': 20,
                'preallocated_vus': 200, 'max_vus': 200, 'nominal_iterations': 100000,
                'measured_iterations': measured, 'setup_requests': total - measured,
                'dropped_iterations': dropped, 'actual_scheduled_iterations': measured + dropped,
                'native_count_time_and_rate_validation': 'Required offline before interpretation; no arbitrary integer tolerance'})
        return out

    def idle(self):
        self.marker('idle-start', duration_seconds=20,
                    rule='After generator completion; final native HTTP completion is reconstructed offline')
        time.sleep(20)
        self.require_live()
        self.marker('idle-end')


def execute(args):
    args.plan = args.plan.resolve()
    args.output = args.output.resolve()
    parent_affinity = sorted(os.sched_getaffinity(0))
    if parent_affinity != [0, 8]:
        raise ValueError('Pin the campaign parent/monitor to observer CPUs with taskset -c 0,8')
    plan = json.loads(args.plan.read_text())
    verify(plan)
    if args.output.exists():
        raise FileExistsError('Refusing to overwrite or resume a completed/partial campaign')
    args.output.mkdir(parents=True)
    args.output_created = True
    dump(args.output / 'plan.json', plan)
    dump(args.output / 'start.json', {'start': baseline.stamp(), 'parent': baseline.proc(os.getpid()),
                                     'parent_affinity': parent_affinity,
                                     'session_bound': plan['session_bound'],
                                     'plan_sha256': sha(args.plan), 'root_grant': args.root_grant})
    # Runtime/diagnostic knobs must not leak from the shell into one comparison arm.
    unexpected = {k: v for k, v in os.environ.items()
                  if k.startswith(('COMPlus_', 'DOTNET_GC', 'DOTNET_Enable', 'DOTNET_Tiered',
                                   'BENCH_CAPTURE', 'BENCH_DIAGNOSTIC'))}
    if unexpected:
        raise ValueError(f'Unset inherited runtime/diagnostic knobs before execution: {sorted(unexpected)}')
    try:
        for item in plan['schedules']:
            arm = item['arm']
            layout = args.output / 'layouts' / arm
            if not layout.exists():
                layout.mkdir(parents=True)
                (layout / 'gateway').symlink_to(plan['arms'][arm]['gateway'], target_is_directory=True)
                for name, source in plan['sources'].items():
                    (layout / name).symlink_to(source['path'], target_is_directory=True)
            baseline.FROZEN = layout.resolve()
            out = args.output / item['name']
            with Session(out, item['delay_ms']) as session:
                dump(out / 'arm.json', item)
                loaded = (out / 'gateway-maps.txt').read_text()
                expected_engine = str(Path(plan['arms'][arm]['gateway']).resolve() / (ENGINE + '.dll'))
                if expected_engine not in loaded:
                    raise ValueError(f'Unexpected loaded execution module path: {expected_engine}')
                session.preflight()
                session.k6('warmup', duration=30, raw=False)
                if item['family'] == 'ordinary':
                    session.k6('ordinary-fixed', workload='ordinary', duration=20, vus=200)
                elif item['delay_ms'] == 0:
                    session.k6('TestQuery', duration=30)
                    session.k6('entities9', workload='entities9', duration=20)
                else:
                    session.k6('TestQuery', duration=30)
                    session.k6('burst', duration=60, mode='ramping', vus=500)
                    session.idle()
            print(f'Completed {item["name"]}', flush=True)
        dump(args.output / 'end.json', {'end': baseline.stamp(), 'completed': True})
    except BaseException as error:
        dump(args.output / 'failure.json', {'end': baseline.stamp(), 'type': type(error).__name__,
                                           'message': str(error)})
        raise


def bounded_execute(args):
    global WORK_DEADLINE
    started = time.monotonic()
    WORK_DEADLINE = started + WORK_SECONDS
    args.output_created = False

    def deadline(signum, frame):
        raise TimeoutError('48-minute campaign work deadline; 120 seconds reserved for owned cleanup')

    previous = signal.signal(signal.SIGALRM, deadline)
    signal.setitimer(signal.ITIMER_REAL, WORK_SECONDS)
    try:
        execute(args)
    finally:
        signal.setitimer(signal.ITIMER_REAL, 0)
        signal.signal(signal.SIGALRM, previous)
        # Covers an alarm arriving while entering the context manager's cleanup.
        if ACTIVE_SESSION is not None:
            ACTIVE_SESSION.close()
        elapsed = time.monotonic() - started
        if args.output_created:
            dump(args.output / 'session-bound.json', {
                'end': baseline.stamp(), 'elapsed_seconds': elapsed,
                'work_seconds': WORK_SECONDS, 'cleanup_reserve_seconds': CLEANUP_RESERVE_SECONDS,
                'total_bound_seconds': WORK_SECONDS + CLEANUP_RESERVE_SECONDS,
                'within_total_bound': elapsed <= WORK_SECONDS + CLEANUP_RESERVE_SECONDS})


def main():
    parser = argparse.ArgumentParser()
    actions = parser.add_subparsers(dest='action', required=True)
    prep = actions.add_parser('prepare')
    prep.add_argument('--survivors', choices=list(ORDERS), required=True)
    prep.add_argument('--plan', type=Path, required=True)
    prep.add_argument('--c2-gateway', type=Path)
    prep.add_argument('--c2-dll')
    prep.add_argument('--c2-pdb')
    run = actions.add_parser('execute')
    run.add_argument('--plan', type=Path, required=True)
    run.add_argument('--output', type=Path, required=True)
    run.add_argument('--root-grant', required=True)
    args = parser.parse_args()
    if args.action == 'prepare':
        prepare(args)
    else:
        signal.signal(signal.SIGTERM, lambda signum, frame: (_ for _ in ()).throw(SystemExit(128 + signum)))
        bounded_execute(args)


if __name__ == '__main__':
    main()
