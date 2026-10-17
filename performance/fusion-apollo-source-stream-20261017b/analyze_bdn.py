#!/usr/bin/env python3
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE / 'results' / 'bdn-shared-campaign'
SHAPES = ['known128', 'unknown4096', 'unknown32768']

timing = {}
allocation = {}
for directory in sorted(ROOT.iterdir()):
    if not directory.is_dir():
        continue
    if '-benchmark-' in directory.name:
        report = json.loads(next(directory.rglob('*-full.json')).read_text())
        timing[directory.name] = {}
        for case in report['Benchmarks']:
            shape = case['Parameters'].split('=', 1)[1]
            timing[directory.name][shape] = {
                'statisticsNanoseconds': case['Statistics'],
                'nativeMemoryDiagnoser': case['Memory'],
                'measurements': case['Measurements'],
            }
    elif '-allocate-' in directory.name:
        allocation[directory.name] = {
            case['Shape']: {
                'bytesPerOperation': [block['allocated'] / block['operations'] for block in case['blocks']],
                'blocks': case['blocks'],
                'method': case['method'],
            }
            for case in json.loads((directory / 'allocation.json').read_text())
        }

comparisons = {}
for candidate, arms in {
    'C7': [('06-benchmark-C7', '05-benchmark-B', '08-benchmark-B'),
           ('10-benchmark-C7', '08-benchmark-B', '11-benchmark-B')],
    'C10': [('07-benchmark-C10', '05-benchmark-B', '08-benchmark-B'),
            ('09-benchmark-C10', '08-benchmark-B', '11-benchmark-B')],
}.items():
    comparisons[candidate] = {}
    for shape in SHAPES:
        ratios = []
        for candidate_arm, before, after in arms:
            candidate_mean = timing[candidate_arm][shape]['statisticsNanoseconds']['Mean']
            ratios.append({
                'candidateArm': candidate_arm,
                'beforeControl': before,
                'afterControl': after,
                'meanNanoseconds': candidate_mean,
                'percentVsBefore': 100 * (candidate_mean / timing[before][shape]['statisticsNanoseconds']['Mean'] - 1),
                'percentVsAfter': 100 * (candidate_mean / timing[after][shape]['statisticsNanoseconds']['Mean'] - 1),
            })
        comparisons[candidate][shape] = ratios

summary = {
    'campaign': json.loads((ROOT / 'campaign.json').read_text()),
    'cleanup': json.loads((ROOT / 'independent-cleanup-verification.json').read_text()),
    'timing': timing,
    'scopedAllocation': allocation,
    'adjacentControlComparisons': comparisons,
    'provisionalVerdict': 'Both candidates pass the bounded micro gate, pending independent review. No CPU or HTTP claim.',
    'limitations': [
        'Synchronous non-seekable memory-backed response streams; no network/async scheduling CPU measurement.',
        'Native MemoryDiagnoser uses default adaptive server GC and process-wide precise allocation accounting.',
        'Supplemental exact current-thread bytes use fixed four heaps and guarded no-GC regions, not timing policy.',
        'Known128 has a large first-observation spread; C10 second arm is 4.1-4.4% higher while first arm is about 1% lower.',
        'Unknown4096 first baseline is slower than later controls; every candidate is still faster than both adjacent controls.',
        'No observations or outliers removed. Native reported allocation includes 0-3 bytes of diagnostic overhead relative to scoped work.',
    ],
}
(HERE / 'results' / 'micro-summary.json').write_text(json.dumps(summary, indent=2) + '\n')

lines = [
    'C7/C10 shared response-reader micro results',
    '',
    'Provisional decision: both pass the bounded micro gate, pending independent review. No HTTP or process CPU result is claimed.',
    '',
    'One serialized campaign completed all 11 processes in 102.5 seconds. Four separately calibrated allocation processes preceded seven native BenchmarkDotNet processes with MemoryDiagnoser. All runtime, binary, operation-count, no-GC, exact calibration, native output and cleanup guards passed. No retries or removed observations.',
    '',
    'Elapsed means in nanoseconds per complete response operation:',
    '',
    '| Arm | known128 | unknown4096 | unknown32768 |',
    '| --- | ---: | ---: | ---: |',
]
for label, cases in timing.items():
    lines.append('| ' + label + ' | ' + ' | '.join(f"{cases[shape]['statisticsNanoseconds']['Mean']:.3f}" for shape in SHAPES) + ' |')
lines += ['', 'Native MemoryDiagnoser allocated bytes per operation (raw JSON values):', '',
          '| Arm | known128 | unknown4096 | unknown32768 |', '| --- | ---: | ---: | ---: |']
for label, cases in timing.items():
    lines.append('| ' + label + ' | ' + ' | '.join(str(cases[shape]['nativeMemoryDiagnoser']['BytesAllocatedPerOperation']) for shape in SHAPES) + ' |')
lines += ['', 'All native Gen0/Gen1/Gen2 raw collection counts are zero and TotalOperations is 1000 for every case. This describes the extra diagnostic workload, not the entire process lifetime.',
          '', 'Separately calibrated scoped allocation bytes per operation (all five blocks identical in each cell):', '',
          '| Arm | known128 | unknown4096 | unknown32768 |', '| --- | ---: | ---: | ---: |']
for label, cases in allocation.items():
    assert all(len(set(cases[shape]['bytesPerOperation'])) == 1 for shape in SHAPES)
    lines.append('| ' + label + ' | ' + ' | '.join(str(cases[shape]['bytesPerOperation'][0]) for shape in SHAPES) + ' |')
lines += ['', 'Exact scoped savings are 328 B per affected response for C7, and 144 B/288 B at 4 KB/32 KB for C10. The known-length 128 B case is unchanged. Native MemoryDiagnoser corroborates these deltas within 0-3 B per-operation diagnostic overhead.',
          '', 'Every affected candidate mean is lower than both adjacent baseline means in both rounds. C7 improves 14.5-30.5%; C10 improves 6.1-22.6% at 4 KB and 7.2-9.1% at 32 KB. These are elapsed reductions, not CPU measurements.',
          '', 'The known128 control is noisy: C10 second-round mean is 4.1-4.4% higher, while its first-round mean is about 1% lower. The 4 KB first baseline is slower than the later baselines. Both caveats remain in the full raw distribution and all adjacent-control comparisons; no rerun or trimming was used.',
          '', 'The fixture uses the actual production reader, fully consumes and disposes results, and seals/disposes each arena. Memory-backed streams complete synchronously. Applicable real HTTP CPU, asynchronous read cost, tails and workload coverage remain acceptance gates. C10 benefit depends on physical arena chunk adjacency; its fallback preserves noncontiguous chunks.',
          '', 'Exact MemoryDiagnoser scope is documented at https://github.com/ChilliCream/graphql-platform/blob/1ef3921f5754001f2a88af30be361a381acc400a/evidence/fusion-apollo-20261017/method-review/memory-diagnoser-scope.txt . Native timing/diagnostic runs use default adaptive server GC; the separately labelled scoped allocation runs use fixed four heaps and guarded 512 MiB no-GC regions with zero collections and exact 523240 B before/after calibrations.',
          '', 'Raw logs, native reports, all measurements, runtime identities, launch records and independent cleanup are in results/bdn-shared-campaign. Machine-readable comparisons and untouched measurement arrays are in results/micro-summary.json. Frozen inputs and source identities are recorded in READY.txt and results/final-preparation.', '']
(HERE / 'MICRO-RESULTS.md').write_text('\n'.join(lines))
print(json.dumps(comparisons, indent=2))
