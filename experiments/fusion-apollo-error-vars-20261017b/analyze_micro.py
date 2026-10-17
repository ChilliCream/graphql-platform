#!/usr/bin/env python3
"""Summarize every valid fixed-policy allocation and default-policy timing observation."""
import argparse
import json
from pathlib import Path
import statistics


def describe(values):
    return {'mean': statistics.mean(values), 'median': statistics.median(values),
            'min': min(values), 'max': max(values), 'raw': values}


parser = argparse.ArgumentParser()
parser.add_argument('campaign', type=Path)
args = parser.parse_args()
launches = json.loads((args.campaign / 'launches.json').read_text())
expected = ['01-allocation-baseline', '02-allocation-candidate', '03-allocation-baseline',
            '04-timing-baseline', '05-timing-candidate', '06-timing-baseline',
            '07-timing-candidate', '08-timing-baseline']
assert [row['label'] for row in launches] == expected
reports = {}
allocation = {}
timing = {}
for launch in launches:
    label = launch['label']
    assert launch['status'] == 'completed' and launch['exitCode'] == 0 and not launch['ownedChildAlive']
    report = json.loads((args.campaign / (label + '.json')).read_text())
    assert report['runtime'] == '.NET 10.0.12' and report['IsServerGC'] and len(report['rows']) == 10
    reports[label] = report
    allocation_mode = report['mode'] == 'allocation'
    config = report['gcConfiguration']
    assert config['GCDynamicAdaptationMode'] == (0 if allocation_mode else 1)
    if allocation_mode:
        assert config['HeapCount'] == 4 and launch['runtimeEnvironment']['DOTNET_GCHeapCount'] == '4'
    else:
        assert 'DOTNET_GCHeapCount' not in launch['runtimeEnvironment']
    for row in report['rows']:
        name = row['shape']['Name']
        blocks = row['blocks']
        assert row['warmupMilliseconds'] == 3000
        if allocation_mode:
            assert len(blocks) == 5
            for block in blocks:
                assert block['noGc'] and block['noCollections'] and block['collections'] == [0, 0, 0]
                assert block['regionMode'] == 4 and block['endError'] is None
                assert block['completed'] == block['iterations'] == 1000
                for side in ['calibrationBefore', 'calibrationAfter']:
                    assert block[side]['ThreadBytes'] == block[side]['TotalBytes'] == 523240
            allocation.setdefault(name, {})[label] = {
                'bytes_per_completed_operation': describe([b['totalBytes'] / b['completed'] for b in blocks]),
                'idle_before_bytes': [b['idleBefore']['Bytes'] for b in blocks],
                'idle_after_bytes': [b['idleAfter']['Bytes'] for b in blocks],
                'idle_before_ms': [b['idleBefore']['Milliseconds'] for b in blocks],
                'idle_after_ms': [b['idleAfter']['Milliseconds'] for b in blocks],
                'warm_iterations': row['warmIterations'],
            }
        else:
            assert len(blocks) == 12
            values = [b['nanosecondsPerOperation'] for b in blocks]
            timing.setdefault(name, {})[label] = {
                'nanoseconds_per_operation': describe(values),
                'first_three_mean_ns': statistics.mean(values[:3]),
                'last_three_mean_ns': statistics.mean(values[-3:]),
                'last_to_first_three_ratio': statistics.mean(values[-3:]) / statistics.mean(values[:3]),
                'collections': [sum(b['gc'][i] for b in blocks) for i in range(3)],
                'collections_by_block': [b['gc'] for b in blocks],
                'warm_iterations': row['warmIterations'],
            }

hypotheses = {'ordinary': 0, 'unique1': 64, 'unique5': 224, 'unique6': 264, 'unique9': 384,
              'duplicate96-to1': 64, 'mixed96-to6': 264, 'batch9': 768,
              'transport9': 0, 'batch-fallback9': 384}
allocation_comparisons = {}
timing_comparisons = {}
for name in hypotheses:
    left = allocation[name]['01-allocation-baseline']['bytes_per_completed_operation']
    candidate = allocation[name]['02-allocation-candidate']['bytes_per_completed_operation']
    right = allocation[name]['03-allocation-baseline']['bytes_per_completed_operation']
    allocation_comparisons[name] = {
        'hypothesized_bytes_saved': hypotheses[name],
        'before_mean': left['mean'], 'candidate_mean': candidate['mean'], 'after_mean': right['mean'],
        'before_minus_candidate_mean': left['mean'] - candidate['mean'],
        'after_minus_candidate_mean': right['mean'] - candidate['mean'],
        'before_minus_candidate_median': left['median'] - candidate['median'],
        'after_minus_candidate_median': right['median'] - candidate['median'],
    }
    controls = [timing[name][label]['nanoseconds_per_operation']['mean']
                for label in ['04-timing-baseline', '06-timing-baseline', '08-timing-baseline']]
    comparisons = []
    for index, label in enumerate(['05-timing-candidate', '07-timing-candidate']):
        value = timing[name][label]['nanoseconds_per_operation']['mean']
        comparisons.append({'launch': label, 'candidate_mean_ns': value,
                            'before_mean_ns': controls[index], 'after_mean_ns': controls[index + 1],
                            'ratio_to_before': value / controls[index],
                            'ratio_to_after': value / controls[index + 1],
                            'over_five_percent_both': value > controls[index] * 1.05 and value > controls[index + 1] * 1.05})
    timing_comparisons[name] = {'control_means_in_time_order_ns': controls,
                              'candidate_comparisons': comparisons,
                              'repeated_over_five_percent_both': all(c['over_five_percent_both'] for c in comparisons)}

result = {'allocation': allocation, 'allocation_comparisons': allocation_comparisons,
          'timing': timing, 'timing_comparisons': timing_comparisons,
          'notes': ['All observations are retained; no trimming or background subtraction.',
                    'Allocation is process-scoped under four fixed heaps and calibrated NoGC regions.',
                    'Fifty-millisecond idle observations are empirical background context, not uncertainty bounds.',
                    'Timing uses natural async execution and disposal under default dynamic GC adaptation.',
                    'Three-second warmup is predeclared, not proof of stationarity. Twelve blocks are within one launch.']}
(args.campaign / 'analysis.json').write_text(json.dumps(result, indent=2) + '\n')
for name, allocation_case in allocation_comparisons.items():
    comparisons = timing_comparisons[name]['candidate_comparisons']
    ratios = [(round(c['ratio_to_before'], 4), round(c['ratio_to_after'], 4)) for c in comparisons]
    print(name, 'allocation means', allocation_case['before_mean'], allocation_case['candidate_mean'],
          allocation_case['after_mean'], 'timing ratios', ratios,
          'repeated adverse', timing_comparisons[name]['repeated_over_five_percent_both'])
