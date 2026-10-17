#!/usr/bin/env python3
"""Diagnose retained constant-arrival samples without starting traffic."""
import argparse
import bisect
import collections
import datetime
import gzip
import json
from pathlib import Path
import statistics


def seconds(value):
    return datetime.datetime.fromisoformat(value.replace('Z', '+00:00')).timestamp()


def stamp(value):
    return datetime.datetime.fromtimestamp(value, datetime.UTC).isoformat()


parser = argparse.ArgumentParser()
parser.add_argument('case', type=Path)
args = parser.parse_args()
iterations = []
durations = []
drops = []
vus = []
setup = []
with gzip.open(args.case / 'samples.json.gz', 'rt') as stream:
    for line in stream:
        row = json.loads(line)
        if row.get('type') != 'Point':
            continue
        metric = row['metric']
        data = row['data']
        tags = data.get('tags', {})
        if tags.get('group') == '::setup':
            if metric == 'http_reqs':
                setup.append((seconds(data['time']), data['value']))
            continue
        if metric in ['vus', 'vus_max']:
            vus.append({'metric': metric, 'time': data['time'], 'value': data['value']})
        if not tags.get('scenario'):
            continue
        when = seconds(data['time'])
        if metric == 'iteration_duration':
            iterations.append((when - data['value'] / 1000, when, data['value']))
        elif metric == 'http_req_duration':
            durations.append((when, data['value']))
        elif metric == 'dropped_iterations':
            drops.append((when, data['value']))

iterations.sort()
starts = sorted(row[0] for row in iterations)
ends = sorted(row[1] for row in iterations)
origin = starts[0]
def overlap(when):
    return bisect.bisect_right(starts, when) - bisect.bisect_right(ends, when)

drop_rows = [{'utc': stamp(when), 'relative_seconds': when - origin, 'count': count,
              'inferred_inflight_iterations': overlap(when)} for when, count in drops]
bins = collections.defaultdict(lambda: {'starts': 0, 'completions': 0, 'drops': 0, 'durations': []})
for start, end, duration in iterations:
    bins[int((start - origin) * 10)]['starts'] += 1
    bins[int((end - origin) * 10)]['completions'] += 1
    bins[int((end - origin) * 10)]['durations'].append(duration)
for when, count in drops:
    bins[int((when - origin) * 10)]['drops'] += count
interval_rows = []
for index, values in sorted(bins.items()):
    latency = values.pop('durations')
    interval_rows.append({'relative_start_seconds': index / 10, **values,
                          'mean_iteration_ms': statistics.mean(latency) if latency else None,
                          'max_iteration_ms': max(latency) if latency else None})
gaps = [(b - a, a, b) for a, b in zip(starts, starts[1:])]
gaps.sort(reverse=True)
summary = json.loads((args.case / 'k6_summary.json').read_text())
drop_total = sum(value for _, value in drops)
assert len(iterations) == summary['metrics']['iterations']['values']['count']
assert drop_total == summary['metrics']['dropped_iterations']['values']['count']
assert len(setup) == 100
report = {
    'case': str(args.case), 'iteration_count': len(iterations), 'drop_count': drop_total,
    'setup_requests': len(setup), 'scheduled_slots_completed_plus_dropped': len(iterations) + drop_total,
    'first_inferred_iteration_start': stamp(origin), 'last_inferred_iteration_start': stamp(starts[-1]),
    'inferred_start_span_seconds': starts[-1] - origin,
    'last_iteration_completion': stamp(ends[-1]), 'execution_span_seconds': ends[-1] - origin,
    'last_setup_completion': stamp(setup[-1][0]), 'gap_setup_to_first_iteration_ms': (origin - setup[-1][0]) * 1000,
    'inferred_max_inflight': max(overlap(when) for when in starts),
    'drop_inflight_histogram': dict(collections.Counter(row['inferred_inflight_iterations'] for row in drop_rows)),
    'dropped_observations': drop_rows, 'vus_samples': vus,
    'hundred_ms_bins': interval_rows,
    'largest_start_gaps': [{'gap_ms': gap * 1000, 'from_utc': stamp(a), 'to_utc': stamp(b),
                            'relative_start_seconds': a - origin, 'inflight_at_gap_start': overlap(a)}
                           for gap, a, b in gaps[:30]],
    'limitations': ['Inferred starts subtract reported iteration_duration from metric callback timestamps.',
                   'Callback timestamps are not exact scheduler/VU acquisition callbacks; inferred concurrency can differ at boundaries.',
                   'VU gauges are sparse snapshots and can miss short peaks.',
                   'No observation is trimmed and no new workload is executed.'],
}
clusters = []
for row in drop_rows:
    if not clusters or row['relative_seconds'] - clusters[-1]['end'] > .001:
        clusters.append({'start': row['relative_seconds'], 'end': row['relative_seconds'],
                         'count': 0, 'inflightMin': 50, 'inflightMax': 0})
    cluster = clusters[-1]
    cluster['end'] = row['relative_seconds']
    cluster['count'] += row['count']
    cluster['inflightMin'] = min(cluster['inflightMin'], row['inferred_inflight_iterations'])
    cluster['inflightMax'] = max(cluster['inflightMax'], row['inferred_inflight_iterations'])
for cluster in clusters:
    cluster['duration_ms'] = (cluster['end'] - cluster['start']) * 1000
    cluster['preceding_large_start_gaps'] = [
        gap for gap in report['largest_start_gaps']
        if gap['relative_start_seconds'] <= cluster['start']
        and cluster['start'] - gap['relative_start_seconds'] < .1]
report['drop_clusters_with_max_1ms_gap'] = clusters
(args.case / 'drop-diagnosis.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps({key: value for key, value in report.items()
                  if key not in ['dropped_observations', 'vus_samples', 'hundred_ms_bins', 'largest_start_gaps',
                                 'drop_clusters_with_max_1ms_gap']}, indent=2))
print('Largest dropped 100ms bins:', sorted(interval_rows, key=lambda row: row['drops'], reverse=True)[:15])
print('Largest start gaps:', report['largest_start_gaps'][:5])
