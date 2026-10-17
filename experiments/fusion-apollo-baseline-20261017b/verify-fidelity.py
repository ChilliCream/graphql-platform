"""Validate captured Federation schema, plan, requests and exact workload identity."""
import collections, hashlib, json
from pathlib import Path
HERE=Path(__file__).resolve().parent
BASE=Path('/workspaces/platform-1/perf/fusion-apollo-baseline-20261017b/fidelity')
assert hashlib.sha256((HERE/'fixture/k6/k6.js').read_bytes()).hexdigest()=='b967285db74541fd9b29ab68522b6f5902de4caf7e43946dab183205bc88ea6e'
schemas=[]
for name in ['Accounts','Inventory','Products','Reviews']:
    actual=json.loads((BASE/(name+'-service.json')).read_text())['data']['_service']['sdl']
    archived=(HERE/f'composition/source-schemas/{name.lower()}/schema.graphqls').read_text()
    assert actual.rstrip('\n')==archived.rstrip('\n')
    assert 'https://specs.apollo.dev/federation/v2.7' in actual
    schemas.append({'name':name,'actual_sha256':hashlib.sha256(actual.encode()).hexdigest(),'archived_sha256':hashlib.sha256(archived.encode()).hexdigest(),'equal_except_terminal_newline':True})
plan=json.loads((BASE/'TestQuery-plan-response.json').read_text())['extensions']['fusion']['operationPlan']
counts=[]
for line in (BASE/'wire.jsonl').read_text().splitlines():
    row=json.loads(line)
    if row['authorization']!='Bearer fidelity-TestQuery':continue
    body=json.loads(row['body'])
    representations={k:len(v) for k,v in body.get('variables',{}).items() if k.endswith('representations')}
    counts.append({'url':row['url'],'entity_selections':len(representations),'representation_counts':representations,'representation_total':sum(representations.values())})
report={'source_schemas':schemas,'plan_node_types':dict(collections.Counter(n['type'] for n in plan['nodes'])),'fetches':counts,'total_fetches':len(counts),'entity_selections':sum(r['entity_selections'] for r in counts),'representations':sum(r['representation_total'] for r in counts)}
assert report['total_fetches']==7 and report['entity_selections']==11 and report['representations']==24
(BASE/'fidelity-verification.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
