import json
from lifecycle import ROOT, request
from workloads import CASES

output=ROOT/'validation';output.mkdir(exist_ok=True)
rows=[]
for case in CASES+[dict(name='omission-control',size=200,supplied=1)]:
    payload=(ROOT/'requests'/f'{case["name"]}.json').read_bytes()
    with request('http://127.0.0.1:5320/graphql',payload) as response:
        body=response.read()
        assert response.status==200
    (output/f'{case["name"]}.body').write_bytes(body)
    data=json.loads(body)
    assert not data.get('errors'),data
    supplied=case.get('supplied',case['size'])
    tags='|'.join(f'value{i}' if i<supplied else '' for i in range(case['size']))
    expected=f'fixed:7:True:1.25:input:42:{tags}'
    assert data['data']=={'a0':expected,'a1':expected},(case['name'],data)
    row=dict(case=case['name'],definitions=case['size'],supplied=supplied,aliases=2,responseBytes=len(body),allForwardedValuesVerified=True)
    rows.append(row);print(json.dumps(row),flush=True)
(ROOT/'validation.json').write_text(json.dumps(rows,indent=2))
