"""Analyze existing complete phases after the user-directed stop, without traffic."""
import importlib.util
import json
from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('campaign_analysis', HERE / 'analyze.py')
analysis = importlib.util.module_from_spec(spec)
spec.loader.exec_module(analysis)

root = Path(sys.argv[1])
plan = json.loads((root / 'plan.json').read_text())
reports = []
omitted = []
entries = [(plan['retained']['arm'], Path(plan['retained']['path']) / plan['retained']['session'], True)]
entries += [(launch, root / launch['name'], False) for launch in plan['schedules']]
for launch, session, retained in entries:
    cases = [name for name in launch['cases']
             if (session / name / 'completion.json').exists()
             and json.loads((session / name / 'completion.json').read_text())['returncode'] == 0]
    omitted.extend({'launch': launch['name'], 'case': name, 'reason': 'No completed measurement before user-directed stop'}
                   for name in launch['cases'] if name not in cases)
    if not cases:
        continue
    cleanup = json.loads((session / 'cleanup.json').read_text())
    assert not cleanup['remaining_owned_pids']
    rows = [json.loads(line) for line in (session / 'process-samples.jsonl').read_text().splitlines()]
    counters, complete = analysis.baseline.load_counters(session / 'counters.json')
    for name in cases:
        case = session / name
        report = analysis.baseline.analyze(case, rows, counters, complete)
        report['arm'] = launch
        report['retained_first_baseline'] = retained
        assert report['setup_requests_excluded'] == 100
        assert report['metric_sums'].get('dropped_iterations', 0) == 0
        reports.append(report)
        if not retained:
            analysis.write(case / 'analysis.json', report)
        print(json.dumps({'launch': launch['name'], 'case': name, 'successes': report['successes'],
                          'cpu_ms': report['gateway_cpu_ms_per_success'],
                          'p99_ms': report['latency_ms']['p99']}), flush=True)

analysis.write(root / 'analysis-completed.json', reports)
analysis.write(root / 'analysis-omitted.json', omitted)
apollo_plan = dict(plan, schedules=[r for r in plan['schedules'] if r['family'] == 'apollo' and r['delay_ms'] == 0])
comparison = analysis.comparisons(apollo_plan, [r for r in reports if r['arm']['family'] == 'apollo'], {})
analysis.write(root / 'comparisons-apollo-zero.json', comparison)
print('All completed phases analyzed; ordinary comparisons remain unbracketed and burst/retention unmeasured.', flush=True)
