#!/usr/bin/env python3
"""Retain all fixed BDN observations and compare arithmetic means to both controls."""
import datetime
import hashlib
import json
from pathlib import Path
import statistics
import sys

root = Path(sys.argv[1])
launches = []
for folder in sorted(path for path in root.iterdir() if path.is_dir()):
    reports = list(folder.rglob('*-full.json'))
    if not reports:
        continue
    if len(reports) != 1:
        raise ValueError(folder)
    report = reports[0]
    data = json.loads(report.read_text())
    rows = {}
    for benchmark in data['Benchmarks']:
        shape = benchmark['Parameters'].removeprefix('Shape=')
        stages = {}
        for mode, stage in [('Workload', 'Actual'), ('Workload', 'Result'), ('Overhead', 'Actual')]:
            values = [m['Nanoseconds'] / m['Operations'] for m in benchmark['Measurements']
                      if m['IterationMode'] == mode and m['IterationStage'] == stage]
            stages[f'{mode}{stage}'] = values
        result = stages['WorkloadResult']
        actual = stages['WorkloadActual']
        if len(result) != 12 or len(actual) != 12:
            raise ValueError(f'Incomplete fixed observations: {folder.name}/{shape}')
        raw = [m for m in benchmark['Measurements']
               if m['IterationMode'] == 'Workload' and m['IterationStage'] in ('Actual', 'Result')]
        if any(m['Operations'] != 1000 or m['Nanoseconds'] <= 0 for m in raw):
            raise ValueError(f'Invalid sample: {folder.name}/{shape}')
        mean = statistics.mean(result)
        if abs(mean - benchmark['Statistics']['Mean']) > 1e-6:
            raise ValueError('Native BDN arithmetic mean mismatch.')
        rows[shape] = {
            'nanosecondsPerOperation': stages,
            'meanNanoseconds': mean,
            'standardDeviationNanoseconds': statistics.stdev(result),
            'minimumNanoseconds': min(result), 'maximumNanoseconds': max(result),
            'last3OverFirst3Mean': statistics.mean(result[-3:]) / statistics.mean(result[:3]),
            'nativeStatistics': benchmark['Statistics'],
            'nativeMemory': benchmark['Memory'],
        }
    if set(rows) != {'ordinary', 'async-batch9', 'async-single9'}:
        raise ValueError('Unexpected case set.')
    launches.append({'label': folder.name, 'arm': folder.name.split('-')[-1],
                     'report': str(report.relative_to(root)),
                     'reportSha256': hashlib.sha256(report.read_bytes()).hexdigest(), 'cases': rows})
if [launch['arm'] for launch in launches] != ['B', 'C6', 'C8', 'B', 'C8', 'C6', 'B']:
    raise ValueError('Unexpected fixed schedule.')
comparisons = []
for index, before, after in [(1, 0, 3), (2, 0, 3), (4, 3, 6), (5, 3, 6)]:
    current = launches[index]
    for shape, row in current['cases'].items():
        ratios = [row['meanNanoseconds'] / launches[c]['cases'][shape]['meanNanoseconds']
                  for c in [before, after]]
        comparisons.append({'label': current['label'], 'arm': current['arm'], 'shape': shape,
                            'controls': [launches[c]['label'] for c in [before, after]],
                            'meanRatios': ratios,
                            'nativeBytesSavedAgainstControls': [launches[c]['cases'][shape]['nativeMemory']['BytesAllocatedPerOperation'] - row['nativeMemory']['BytesAllocatedPerOperation'] for c in [before, after]],
                            'overFivePercentAgainstBoth': all(r > 1.05 for r in ratios)})
summary = {
    'generatedAt': datetime.datetime.now(datetime.UTC).isoformat(),
    'method': 'BDN elapsed time; arithmetic mean of all twelve native WorkloadResult observations, no outlier removal. Raw WorkloadActual and overhead retained.',
    'actualObservationCount': sum(len(row['nanosecondsPerOperation']['WorkloadActual'])
                                  for launch in launches for row in launch['cases'].values()),
    'launches': launches, 'comparisons': comparisons,
    'repeatedBothControlFailures': [
        {'arm': arm, 'shape': shape}
        for arm in ['C6', 'C8'] for shape in ['ordinary', 'async-batch9', 'async-single9']
        if sum(c['overFivePercentAgainstBoth'] for c in comparisons if c['arm'] == arm and c['shape'] == shape) >= 2
    ],
    'limitations': [
        'BDN elapsed is not process CPU; no direct CPU claim.',
        'Native MemoryDiagnoser uses precise process-wide allocated-byte delta for one extra 1000-operation diagnostic iteration after forced GC. It clamps negative deltas to zero.',
        'Native memory includes whole-process background allocation and is not an exact source-array count. Supplementary fixed-heap allocation stopped on 4656-byte process calibration contamination; only prior validated partial blocks are corroborative.',
        'Fixed 1000-operation iterations are below BDN recommended 100ms minimum; native warnings retained.',
        'Only two candidate launches; all twelve observations retained without trimming; compare both actual adjacent controls.',
        'Correctness handshake proves one actual entity suspension and signal waiter. Measured Task.Yield does not guarantee every operation registers a waiter before producer resumes.'
    ],
}
(root / 'analysis.json').write_text(json.dumps(summary, indent=2) + '\n')
print(json.dumps({'actualObservationCount': summary['actualObservationCount'],
                  'repeatedBothControlFailures': summary['repeatedBothControlFailures'],
                  'comparisons': comparisons}, indent=2))
