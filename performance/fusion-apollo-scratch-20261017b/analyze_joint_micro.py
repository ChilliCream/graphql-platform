#!/usr/bin/env python3
import json
from pathlib import Path
import statistics
import sys

root = Path(sys.argv[1])
rows = []
for path in sorted((root / 'results').glob('timing-*.json')):
    data = json.loads(path.read_text())
    row = {'arm': path.stem.split('-')[-1], 'index': int(path.stem.split('-')[1]), 'cases': {}}
    for case in data['results']:
        values = case['values']
        row['cases'][case['case']] = {
            'mean': statistics.mean(values), 'median': statistics.median(values),
            'first5Mean': statistics.mean(values[:5]), 'last5Mean': statistics.mean(values[-5:]),
            'min': min(values), 'max': max(values), 'gcBefore': case['gcBefore'], 'gcAfter': case['gcAfter']
        }
    rows.append(row)
summary = {'launches': rows, 'cases': {}}
for case in rows[0]['cases']:
    group = {arm: [row['cases'][case]['mean'] for row in rows if row['arm'] == arm] for arm in ['B', 'C1', 'C3']}
    adjacent = []
    for row in rows:
        if row['arm'] == 'B':
            continue
        left = max((r for r in rows if r['arm'] == 'B' and r['index'] < row['index']), key=lambda r: r['index'])
        right = min((r for r in rows if r['arm'] == 'B' and r['index'] > row['index']), key=lambda r: r['index'])
        adjacent.append({'arm': row['arm'], 'index': row['index'], 'left': row['cases'][case]['mean'] / left['cases'][case]['mean'], 'right': row['cases'][case]['mean'] / right['cases'][case]['mean']})
    summary['cases'][case] = {'means': group, 'meanRatios': {arm: statistics.mean(group[arm]) / statistics.mean(group['B']) for arm in ['C1', 'C3']}, 'adjacentRatios': adjacent}
(root / 'timing-summary.json').write_text(json.dumps(summary, indent=2) + '\n')
print(json.dumps(summary['cases']['testquery_sequence'], indent=2))
