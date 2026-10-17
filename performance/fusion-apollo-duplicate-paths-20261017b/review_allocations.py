#!/usr/bin/env python3
"""Validate every scoped allocation observation and the independent no-GC diagnostics."""
import json
from pathlib import Path
import sys
root, output = map(Path, sys.argv[1:])
rows = []
by_case = {}
for path in sorted((root/'results').glob('allocation-*.json')):
    if path.stem.split('-')[-1] not in ('B', 'C3'):
        continue
    data = json.loads(path.read_text())
    diagnostics = [json.loads(line) for line in path.with_suffix('.stderr').read_text().splitlines() if line.startswith('{')]
    ending = {d['case']: d for d in diagnostics if d['phase'] == 'after-region'}
    assert len(ending) == len(data['results']) == 13
    arm = path.stem.split('-')[-1]
    for result in data['results']:
        diagnostic = ending[result['case']]
        assert diagnostic['gcRegionStart'] == diagnostic['gcRegionEnd']
        assert diagnostic['regionEnded'] and diagnostic['modeBeforeEnd'] == 'NoGCRegion'
        assert diagnostic['calibrationBefore'] == diagnostic['calibrationAfter'] == 127 * (4096 + 24)
        assert diagnostic['completedObservations'] == 4
        assert len(result['values']) == 4 and len(set(result['values'])) == 1
        assert result['values'] == diagnostic['values']
        by_case.setdefault(result['case'], {'B': set(), 'C3': set()})[arm].add(result['values'][0])
        rows.append({'file':path.name, 'case':result['case'], 'bytes':result['values'][0],
                     'observations':4, 'requests_per_observation':16, 'validated':True})
summary = {}
for name, arms in by_case.items():
    assert len(arms['B']) == len(arms['C3']) == 1
    baseline, candidate = next(iter(arms['B'])), next(iter(arms['C3']))
    summary[name] = {'baseline_bytes':baseline, 'candidate_bytes':candidate,
                     'saved_bytes':baseline-candidate, 'relative_percent':100*(candidate/baseline-1)}
output.write_text(json.dumps({'scope':'current-thread Execute plus Clean only; controlled no-GC region; excludes Prepare and arena disposal', 'summary':summary, 'validated_rows':rows},indent=2)+'\n')
print(json.dumps(summary,indent=2))
