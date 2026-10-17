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
import urllib.request
import urllib.parse

HERE = Path(__file__).resolve().parent
ROOT = Path('/workspaces/platform-1')
BASE = ROOT / 'perf/fusion-apollo-baseline-20261017b'
BASELINE_COMMIT = 'ef9fc1159a5a7a980754ebd42b511030320482cd'
CANDIDATES = {'C9': {'gateway': '/workspaces/platform-1/perf/fusion-apollo-compact-path-build-20261017b/frozen-http/gateway', 'expected': {'.dll': '91622b981639bd0304a050866de32f55371087b25b119398421d591b244e145a', '.pdb': '3fff4ea0420288b5da35f01461aa224a6e4f46b64667cab6ddda79c2d8dfa5fd'}}, 'C7': {'gateway': '/workspaces/platform-1/perf/fusion-apollo-source-stream-20261017b-43a8dca2/gateway-overlays/C7', 'expected': {'.dll': 'c651030acc2ca721ee56a9bb0d85290f432cfeeb675d5b45f359bf33117be052', '.pdb': 'ef1ed395c26a95c5e089460d2564499c238a66239a526bb2e359e921c00c0215'}}, 'C10': {'gateway': '/workspaces/platform-1/perf/fusion-apollo-source-stream-20261017b-43a8dca2/gateway-overlays/C10', 'expected': {'.dll': 'f6c74c8d89833eccca5fa5bfb5643c925e8ba34bc72a8f10e8982432072904b8', '.pdb': '868ba784ee88f8bec76859b133d57604ae691c3613a0c0bc3e6a0393ee2f414b'}}}
ENGINE = 'HotChocolate.Fusion.Execution'
ORIGINAL_K6 = 'b967285db74541fd9b29ab68522b6f5902de4caf7e43946dab183205bc88ea6e'
SOURCES = ['Accounts', 'Inventory', 'Products', 'Reviews']
WORK_SECONDS = None
CLEANUP_RESERVE_SECONDS = 120
WORK_DEADLINE = None
ACTIVE_SESSION = None
def comparison_order(survivors):
    labels = survivors.split(',')
    if not labels or len(labels) != len(set(labels)) or any(label not in CANDIDATES for label in labels):
        raise ValueError('Use a unique ordered list of registered independent micro-qualified arms')
    return ['B', *labels, 'B', *reversed(labels), 'B']


def work_seconds(survivors, stage):
    # The final plan records an aggregate bound including startup/setup margin.
    per_launch = 160 if stage == 'zero' else 200
    return len(comparison_order(survivors)) * per_launch + 60


def stage_schedule(survivors, stage):
    delay = 0 if stage == 'zero' else 4
    cases = ['TestQuery', 'entities9', 'ordinary-fixed'] if delay == 0 else ['TestQuery', 'burst']
    return [{'family': 'combined', 'delay_ms': delay, 'index': i, 'arm': arm,
             'name': f'delay{delay}-{i:02d}-{arm}', 'cases': cases}
            for i, arm in enumerate(comparison_order(survivors))]


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
    order = comparison_order(args.survivors)
    args.plan.parent.mkdir(parents=True, exist_ok=True)
    control = hashes(BASE / 'frozen/gateway')
    arms = {'B': {'gateway': str(BASE / 'frozen/gateway'), 'hashes': control}}
    for label in args.survivors.split(','):
        candidate = CANDIDATES[label]
        gateway = Path(candidate['gateway'])
        arms[label] = {'gateway': str(gateway), 'hashes': validate_candidate(
            label, gateway, control, candidate['expected'])}
    if sha(HERE / 'vendor/fixture/k6/k6.js') != ORIGINAL_K6:
        raise ValueError('Original benchmark driver changed')
    if not control or not all(hashes(BASE / 'frozen' / source) for source in SOURCES):
        raise ValueError('Missing frozen fixture')
    plan = {
        'created': baseline.stamp(), 'baseline_commit': BASELINE_COMMIT,
        'source_commit': '43a8dca2bd479ddc411d832ed97e42d9fcc8bd85',
        'survivors': args.survivors, 'order_per_delay': order, 'stage': args.stage,
        'delays_ms': [0 if args.stage == 'zero' else 4],
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
        'schedules': stage_schedule(args.survivors, args.stage),
        'prior_evidence': 'Fresh matched cohort; no retained old controls or stacked candidate engines',
        'execution_authorized': False,
        'session_bound': {'work_seconds': work_seconds(args.survivors, args.stage),
                          'cleanup_reserve_seconds': CLEANUP_RESERVE_SECONDS,
                          'total_seconds': work_seconds(args.survivors, args.stage) + CLEANUP_RESERVE_SECONDS},
    }
    dump(args.plan, plan)
    print(f'Prepared {len(plan["schedules"])} launches; no HTTP executed: {args.plan}')


def verify(plan):
    if plan['session_bound'] != {'work_seconds': work_seconds(plan['survivors'], plan['stage']),
                                 'cleanup_reserve_seconds': CLEANUP_RESERVE_SECONDS,
                                 'total_seconds': work_seconds(plan['survivors'], plan['stage']) + CLEANUP_RESERVE_SECONDS}:
        raise ValueError('Whole-session time bound changed')
    if plan['stage'] not in ['zero', 'delayed'] or plan['order_per_delay'] != comparison_order(plan['survivors']) or plan['delays_ms'] != [0 if plan['stage'] == 'zero' else 4]:
        raise ValueError('Schedule differs from the reviewed bounded orders')
    expected = stage_schedule(plan['survivors'], plan['stage'])
    if plan['schedules'] != expected:
        raise ValueError('Launch schedule was altered')
    if hashes(HERE) != plan['runner_sources']:
        raise ValueError('Runner, oracle, workload or analysis source changed after freeze')
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

    def capture_source_response_shapes(self):
        self.marker('source-response-shape-capture-start')
        fixture = json.loads((HERE / 'source-response-preflight.json').read_text())
        if len(fixture['requests']) != 7:
            raise ValueError('Expected exactly seven established original source requests')
        directory = self.out / 'source-response-shapes'
        directory.mkdir()
        captured = []
        for index, original in enumerate(fixture['requests']):
            url = urllib.parse.urlsplit(original['url'])
            if url.scheme != 'http' or url.hostname != 'localhost' or url.port not in [5221, 5222, 5223, 5224] or url.path != '/graphql':
                raise ValueError('Source capture target differs from the local benchmark fixture')
            content_types = ['application/graphql-response+json', 'application/json', 'application/jsonl', 'text/event-stream']
            if original['accept_policy'] == 'batching':
                content_types = ['application/jsonl', 'text/event-stream', 'application/graphql-response+json', 'application/json']
            accept = ', '.join(value + '; charset=utf-8' for value in content_types)
            request = urllib.request.Request(original['url'], data=original['body'].encode(), method='POST',
                headers={'Content-Type': 'application/json; charset=utf-8', 'Accept': accept,
                         'Authorization': f'Bearer source-shape-{index}'})
            with urllib.request.urlopen(request, timeout=5) as response:
                body = response.read(2 * 1024 * 1024 + 1)
                if len(body) > 2 * 1024 * 1024 or response.status != 200:
                    raise ValueError('Source shape capture failed status or bounded response size')
                headers = {key: response.headers.get(key) for key in
                           ['Content-Length', 'Content-Type', 'Transfer-Encoding', 'Content-Encoding']}
                decoded = json.loads(body)
                if decoded.get('errors') or 'data' not in decoded:
                    raise ValueError('Source shape capture response has GraphQL errors or no data')
                (directory / f'{index:02}-response.json').write_bytes(body)
                captured.append({'index': index, 'url': original['url'], 'accept_policy': original['accept_policy'],
                    'protocol_version': response.version, 'status': response.status, 'headers': headers,
                    'body_bytes': len(body), 'body_sha256': hashlib.sha256(body).hexdigest(),
                    'request_body_sha256': hashlib.sha256(original['body'].encode()).hexdigest()})
        dump(directory / 'summary.json', {'responses': captured,
             'scope': 'Seven direct fixture response captures during first baseline preflight, followed by30s conditioning. urllib HTTP transport differs from the gateway client; headers and sizes are observed for this capture, not instrumented gateway responses. Physical arena chunk adjacency is unobserved. No sensitive request/response header values retained.'})
        self.marker('source-response-shape-capture-end')

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
                  if k.startswith(('COMPlus_', 'CORECLR_', 'DOTNET_GC', 'DOTNET_Enable', 'DOTNET_Tiered',
                                   'DOTNET_ReadyToRun', 'DOTNET_PROCESSOR_COUNT',
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
                if item['index'] == 0:
                    session.capture_source_response_shapes()
                session.k6('warmup', duration=30, raw=False)
                if item['delay_ms'] == 0:
                    session.k6('TestQuery', duration=30)
                    session.k6('entities9', workload='entities9', duration=20)
                    session.k6('ordinary-fixed', workload='ordinary', duration=20, vus=200)
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
    global WORK_DEADLINE, WORK_SECONDS
    frozen_plan = json.loads(args.plan.read_text())
    WORK_SECONDS = work_seconds(frozen_plan['survivors'], frozen_plan['stage'])
    started = time.monotonic()
    WORK_DEADLINE = started + WORK_SECONDS
    args.output_created = False

    def deadline(signum, frame):
        raise TimeoutError(f'{WORK_SECONDS}-second campaign work deadline; 120 seconds reserved for owned cleanup')

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
    prep.add_argument('--survivors', required=True)
    prep.add_argument('--stage', choices=['zero', 'delayed'], required=True)
    prep.add_argument('--plan', type=Path, required=True)
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
