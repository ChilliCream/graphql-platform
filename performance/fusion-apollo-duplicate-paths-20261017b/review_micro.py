#!/usr/bin/env python3
"""Independently compare C3 launches with both adjacent controls, retaining all observations."""
import hashlib
import json
from pathlib import Path
import statistics
import sys

root = Path(sys.argv[1])
out = Path(sys.argv[2])
inputs = sorted((root / 'results').glob('timing-*.json'))
runs = {}
identities = {}
for path in inputs:
    data = json.loads(path.read_text())
    index = int(path.stem.split('-')[1])
    arm = path.stem.split('-')[2]
    identities[path.name] = hashlib.sha256(path.read_bytes()).hexdigest()
    if arm not in ('B', 'C3'):
        continue
    assert data['mode'] == 'timing' and data['observations'] == 20
    runs[index] = {'arm': arm, 'sha256': data['sha256'], 'utc': data['utc'], 'cases': {}}
    for result in data['results']:
        values = result['values']
        assert len(values) == 20
        mean = statistics.mean(values)
        assert abs(mean - result['arithmeticMean']) < 1e-6
        runs[index]['cases'][result['case']] = {
            'mean_ns': mean, 'median_block_ns': statistics.median(values),
            'min_block_ns': min(values), 'max_block_ns': max(values),
            'first5_mean_ns': statistics.mean(values[:5]),
            'last5_mean_ns': statistics.mean(values[-5:]),
            'largest_block_fraction_of_total_measured_time': max(values) / sum(values),
            'gc_generation_deltas': [b-a for a, b in zip(result['gcBefore'], result['gcAfter'])]
        }
cases = {}
for case in runs[0]['cases']:
    controls = {i: runs[i]['cases'][case]['mean_ns'] for i in [0, 3, 6, 9]}
    candidates = {i: runs[i]['cases'][case]['mean_ns'] for i in [2, 4, 8]}
    pairs = []
    for index, before, after in [(2, 0, 3), (4, 3, 6), (8, 6, 9)]:
        value = candidates[index]
        pairs.append({'candidate_index': index, 'before_index': before, 'after_index': after,
                      'relative_to_before_percent': 100 * (value / controls[before] - 1),
                      'relative_to_after_percent': 100 * (value / controls[after] - 1)})
    cases[case] = {'controls_ns': controls, 'candidates_ns': candidates,
                   'control_envelope_ns': [min(controls.values()), max(controls.values())],
                   'candidate_envelope_ns': [min(candidates.values()), max(candidates.values())],
                   'adjacent_comparisons': pairs}
report = {'input_sha256': identities, 'all_observations_retained': True,
          'independent_units': 'process launches; blocks within launches are not independent runs',
          'runs': runs, 'cases': cases}
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps(cases['testquery_sequence'], indent=2))
