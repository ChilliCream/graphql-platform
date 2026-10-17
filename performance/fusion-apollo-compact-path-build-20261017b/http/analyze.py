"""Analyze a retained campaign under the shared lock; never starts traffic."""
import argparse
import collections
import gzip
import importlib.util
import json
from pathlib import Path
import statistics

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('baseline_analysis', HERE / 'vendor/analyze.py')
baseline = importlib.util.module_from_spec(spec)
spec.loader.exec_module(baseline)


def write(path, value):
    path.write_text(json.dumps(value, indent=2) + '\n')


def describe(values):
    values = sorted(values)
    return {'observations': len(values), 'min': min(values), 'max': max(values),
            'mean': statistics.mean(values), 'median': statistics.median(values),
            'p95': baseline.quantile(values, .95)}


def retention(session, rows, counters, burst):
    boundaries = {r['name']: r for r in map(json.loads, (session / 'boundaries.jsonl').read_text().splitlines())}
    before = boundaries['burst-before-setup']['epoch']
    idle_start = boundaries['idle-start']['epoch']
    idle_end = boundaries['idle-end']['epoch']
    if idle_end - idle_start < 20:
        raise ValueError('Incomplete idle observer window')
    windows = {'before_setup_last_1s': (before - 1, before),
               'burst_including_setup': (before, idle_start),
               'idle_full': (idle_start, idle_end),
               'idle_final_5s': (idle_end - 5, idle_end)}
    report = {'boundaries': boundaries, 'last_native_http_completion_epoch': burst['window_end_epoch'],
              'generator_completion_after_last_http_seconds': idle_start - burst['window_end_epoch'],
              'windows': {}, 'notes': [
                  'RSS is process residency, not retained managed heap.',
                  'GC heap and committed gauges can describe the last GC and remain stale during idle.',
                  'No forced GC was requested. Rate values are interval observations, not cumulative totals.']}
    for name, (start, end) in windows.items():
        samples = [r['processes']['gateway'] for r in rows
                   if start <= r['time'] <= end and 'gateway' in r['processes']]
        if not samples:
            raise ValueError(f'Missing memory observations: {session}, {name}')
        memory = [c for c in counters if start <= baseline.seconds(c['timestamp']) <= end
                  and any(term in c['name'].lower() for term in ['gc.', 'working_set', 'working-set', 'memory'])]
        report['windows'][name] = {'start_epoch': start, 'end_epoch': end,
                                    'rss_bytes': describe([s['rss_bytes'] for s in samples]),
                                    'threads': describe([s['threads'] for s in samples]),
                                    'raw_runtime_memory_observations': memory}
    return report


def burst_stages(case, report):
    # k6's first observed request is close to scenario start; preserve that alignment limit.
    origin = report['iteration_window_sensitivity']['start_epoch']
    stages = collections.defaultdict(list)
    with gzip.open(case / 'samples.json.gz', 'rt') as stream:
        for line in stream:
            row = json.loads(line)
            if row.get('type') != 'Point' or row['metric'] != 'http_req_duration':
                continue
            data = row['data']
            tags = data.get('tags', {})
            if tags.get('group') == '::setup' or not tags.get('scenario'):
                continue
            relative = baseline.seconds(data['time']) - origin
            label = '0-10s' if relative < 10 else '10-50s' if relative < 50 else '50-60s' if relative < 60 else 'graceful-drain'
            stages[label].append(data['value'])
    return {'origin_epoch': origin,
            'alignment': 'First observed iteration start; completion-assigned stage bins, not exact scheduler callbacks.',
            'stages': {name: {'count': len(values), 'mean_ms': statistics.mean(values),
                              'p95_ms': baseline.quantile(sorted(values), .95),
                              'p99_ms': baseline.quantile(sorted(values), .99),
                              'p999_ms': baseline.quantile(sorted(values), .999),
                              'max_ms': max(values)} for name, values in stages.items()}}


def iteration_boundaries(case):
    first = None
    last_start = None
    last_end = None
    count = 0
    with gzip.open(case / 'samples.json.gz', 'rt') as stream:
        for line in stream:
            row = json.loads(line)
            if row.get('type') != 'Point' or row.get('metric') != 'iteration_duration':
                continue
            data = row['data']
            tags = data.get('tags', {})
            if tags.get('group') == '::setup' or not tags.get('scenario'):
                continue
            end = baseline.seconds(data['time'])
            start = end - data['value'] / 1000
            first = start if first is None else min(first, start)
            last_start = start if last_start is None else max(last_start, start)
            last_end = end if last_end is None else max(last_end, end)
            count += 1
    return {'iteration_duration_observations': count,
            'inferred_first_iteration_start_epoch': first,
            'inferred_last_iteration_start_epoch': last_start,
            'last_iteration_completion_epoch': last_end,
            'semantics': 'Native iteration_duration completion callback minus its duration; inferred iteration starts, not exact executor scheduling deadlines.'}


def metrics(report):
    return {'gateway_cpu_ms_per_success': report['gateway_cpu_ms_per_success'],
            'throughput_success_per_second': report['throughput_success_per_second'],
            **{f'latency_{key}_ms': value for key, value in report['latency_ms'].items()}}


def comparisons(plan, reports, memory):
    result = []
    indexed = {(r['arm']['family'], r['arm']['delay_ms'], r['arm']['index'], Path(r['case']).name): r for r in reports}
    all_launches = plan['schedules']
    for family, delay in sorted({(r['family'], r['delay_ms']) for r in all_launches}):
        launches = sorted([r for r in all_launches if r['family'] == family and r['delay_ms'] == delay],
                          key=lambda r: r['index'])
        controls = [r for r in launches if r['arm'] == 'B']
        for launch in launches:
            if launch['arm'] == 'B':
                continue
            before = max(c['index'] for c in controls if c['index'] < launch['index'])
            after = min(c['index'] for c in controls if c['index'] > launch['index'])
            for workload in launch['cases']:
                candidate = indexed[(family, delay, launch['index'], workload)]
                left = indexed[(family, delay, before, workload)]
                right = indexed[(family, delay, after, workload)]
                candidate_metrics = metrics(candidate)
                controls_metrics = [(c['name'], metrics(indexed[(family, delay, c['index'], workload)])) for c in controls]
                entry = {'launch': launch, 'workload': workload,
                         'adjacent_controls': [left['arm']['name'], right['arm']['name']],
                         'candidate_start_epoch': candidate['window_start_epoch'],
                         'seconds_after_previous_control_end': candidate['window_start_epoch'] - left['window_end_epoch'],
                         'seconds_before_next_control_start': right['window_start_epoch'] - candidate['window_end_epoch'],
                         'previous_control_is_retained_first_baseline': left.get('retained_first_baseline', False),
                         'metrics': {}}
                for name, value in candidate_metrics.items():
                    left_value = metrics(left)[name]
                    right_value = metrics(right)[name]
                    all_values = [values[name] for _, values in controls_metrics]
                    entry['metrics'][name] = {
                        'candidate': value, 'before': left_value, 'after': right_value,
                        'relative_to_before': value / left_value - 1,
                        'relative_to_after': value / right_value - 1,
                        'relative_to_adjacent_mean': value / ((left_value + right_value) / 2) - 1,
                        'full_control_min': min(all_values), 'full_control_max': max(all_values),
                        'control_time_order': [{'launch': label, 'value': values[name]} for label, values in controls_metrics],
                    }
                if workload == 'burst':
                    entry['retention'] = {label: memory[label]['windows'] for label in
                                          [left['arm']['name'], launch['name'], right['arm']['name']]}
                result.append(entry)
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('results', type=Path)
    args = parser.parse_args()
    root = args.results
    plan = json.loads((root / 'plan.json').read_text())
    reports = []
    memory = {}
    entries = [(launch, root / launch['name'], False) for launch in plan['schedules']]
    for launch, session, retained in entries:
        if not (session / 'cleanup.json').exists():
            raise ValueError(f'Incomplete campaign session: {session}')
        cleanup = json.loads((session / 'cleanup.json').read_text())
        if cleanup['remaining_owned_pids']:
            raise ValueError(f'Owned process survived cleanup: {session}')
        rows = [json.loads(line) for line in (session / 'process-samples.jsonl').read_text().splitlines()]
        counters, complete = baseline.load_counters(session / 'counters.json')
        for workload in launch['cases']:
            case = session / workload
            if json.loads((case / 'completion.json').read_text())['returncode'] != 0:
                raise ValueError(f'Failed case: {case}')
            report = baseline.analyze(case, rows, counters, complete)
            report['arm'] = launch
            report['retained_first_baseline'] = retained
            setup = 1000 if workload == 'burst' else 100
            if report['setup_requests_excluded'] != setup or report['metric_sums'].get('dropped_iterations', 0) != 0:
                raise ValueError(f'Setup/dropped request accounting: {case}')
            if workload == 'ordinary-fixed':
                summary = json.loads((case / 'k6_summary.json').read_text())['metrics']
                if summary.get('dropped_iterations', {}).get('values', {}).get('count', 0) != 0:
                    raise ValueError(f'Ordinary summary drops: {case}')
                report['offered_load'] = {
                    'configured_rate_per_second': 5000, 'configured_duration_seconds': 20,
                    'nominal_iterations': 100000, 'preallocated_vus': 200, 'max_vus': 200,
                    'completed_iterations': report['successes'], 'dropped_iterations': 0,
                    'actual_scheduled_iterations': report['successes'],
                    'native_window_seconds': report['window_seconds'],
                    'achieved_success_rate_per_second': report['throughput_success_per_second'],
                    'count_contract': 'Exact native/summary HTTP=iterations=successes; summary total HTTP adds100setup; zero drops/errors. No integer tolerance; rate/time departure requires interpretation.',
                    **iteration_boundaries(case)}
                report['notes'].append('Callback timestamps do not identify exact runtime instrument intervals. Initial failed ordinary evidence showed an apparent one-second sequence lag; retain all interval/OS mismatches without shifting primary native CPU windows.')
            if workload == 'burst':
                report['burst_stages'] = burst_stages(case, report)
                memory[launch['name']] = retention(session, rows, counters, report)
                write(session / 'retention-analysis.json', memory[launch['name']])
            reports.append(report)
            if not retained:
                write(case / 'analysis.json', report)
            print(launch['name'], workload, report['successes'], flush=True)
    write(root / 'analysis.json', reports)
    write(root / 'comparisons.json', comparisons(plan, reports, memory))
    write(root / 'retention-analysis.json', memory)


if __name__ == '__main__':
    main()
