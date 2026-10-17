import json
import pathlib

ROOT = pathlib.Path(__file__).resolve().parent
CASES = []

def make(count, nullable=False, supplied=None):
    definitions = ','.join(f'$v{i}:String'+('' if nullable else '!') for i in range(count))
    tags = ','.join(f'$v{i}' for i in range(count))
    arguments = f'text:"fixed",number:7,flag:true,ratio:1.25,input:{{text:"input",nested:{{count:42,tags:[{tags}]}}}}'
    query = f'query Variables({definitions}){{a0:shapeEcho({arguments}) a1:inventoryShapeEcho({arguments})}}'
    return dict(query=query, variables={f'v{i}':f'value{i}' for i in range(count if supplied is None else supplied)})

(ROOT/'requests').mkdir(exist_ok=True)
for count in [50,100,200]:
    payload = make(count)
    case = dict(name=f'variables-{count}',kind='variables',size=count,vus=4,rate=100,timeUnit='1s',**payload)
    CASES.append(case)
    (ROOT/'requests'/f'{case["name"]}.json').write_text(json.dumps(payload,separators=(',',':')))
(ROOT/'requests/omission-control.json').write_text(json.dumps(make(200,True,1),separators=(',',':')))
(ROOT/'cases.json').write_text(json.dumps(CASES,indent=2))
