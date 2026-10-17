import collections, json, pathlib, urllib.request
from lifecycle import ROOT, request
from workloads import CASES

output=ROOT/'validation'
output.mkdir(exist_ok=True)
rows=[]
for case in CASES:
    name,kind,n=case['name'],case['kind'],case['size']
    accept='multipart/mixed; incrementalSpec=v0.2' if kind=='defer' else 'application/graphql-response+json'
    payload=(ROOT/'requests'/f'{name}.json').read_bytes()
    with request('http://127.0.0.1:5320/graphql',payload,accept) as response:
        body=response.read()
        (output/f'{name}.body').write_bytes(body)
        headers=dict(response.headers)
        assert response.status==200,(name,response.status)
    row=dict(name=name,bytes=len(body),headers=headers,errors=0)
    if kind=='defer':
        boundary=headers['Content-Type'].split('boundary=')[1].split(';')[0].strip('"')
        parts=[]
        for part in body.split(('--'+boundary).encode()):
            if b'\r\n\r\n' in part:
                parts.append(json.loads(part.split(b'\r\n\r\n',1)[1].strip()))
        (output/f'{name}.parts.json').write_text(json.dumps(parts,indent=2))
        assert all(not p.get('errors') for p in parts),parts
        pending=[p for part in parts for p in part.get('pending',[])]
        labels=sorted(p['label'] for p in pending)
        assert labels==[f'd{i}' for i in range(n)],labels
        assert parts[-1]['hasNext'] is False
        completed=[p['id'] for part in parts for p in part.get('completed',[])]
        assert sorted(completed)==sorted(p['id'] for p in pending)
        patches=[p for part in parts for p in part.get('incremental',[])]
        allkeys=set(k for p in patches for k in p.get('data',{}))
        assert allkeys=={f'p{i}' for i in range(n)},allkeys
        for patch in patches:
            for key,value in patch.get('data',{}).items():
                assert value==[{'upc':'1','name':'Product','inStock':True,'shippingEstimate':0}],(key,value)
        row.update(labels=labels,completedIds=completed,parts=len(parts),patches=len(patches),finalHasNext=False)
    else:
        obj=json.loads(body)
        assert not obj.get('errors'),(name,obj)
        data=obj['data']
        if kind=='variables':
            expected={f'a{i}':f'value{i}:{i}:{"True" if i%2==0 else "False"}:{i+.25}:input{i}:{i}' for i in range(n//5)}
            assert data==expected,(name,data,expected)
            row.update(variables=n,forwardedVariables=n,aliases=len(data),allEchoValuesVerified=True)
        elif kind in ['plain','lookup']:
            values=data['shapeProducts']
            assert len(values)==n,(name,len(values))
            fields={'upc','name','price','weight'}|({'inStock','shippingEstimate'} if kind=='lookup' else set())
            for i,value in enumerate(values,1):
                assert set(value)==fields,(name,i,value)
                assert value['upc']==str(i) and value['name']=='Product' and value['price']==i and value['weight']==i,(name,i,value)
                if kind=='lookup':
                    assert value['inStock']==(i not in [2,3,4,8]),(name,i,value)
                    assert value['shippingEstimate']==(0 if i>1000 else i//2),(name,i,value)
            row.update(listCount=n,fields=sorted(fields),allValuesVerified=True)
        else:
            counts=collections.Counter()
            def check(value):
                if isinstance(value,list):
                    for v in value: check(v)
                elif isinstance(value,dict):
                    keys=set(value)
                    if keys=={'users','topProducts'}:
                        assert len(value['users'])>0 and len(value['topProducts'])==5
                    elif 'upc' in keys:
                        assert keys in [set('inStock name price shippingEstimate upc weight'.split()),set('inStock name price shippingEstimate upc weight reviews'.split())],keys
                        counts['products']+=1
                        assert all(value[k] is not None for k in keys)
                    elif 'username' in keys:
                        assert keys==set('id username name reviews'.split()),keys
                        counts['users']+=1
                    else:
                        assert keys=={'id','body','product'} or keys=={'id','body','author'},keys
                        counts['reviews']+=1
                    for v in value.values():
                        if isinstance(v,(dict,list)): check(v)
            check(data)
            assert counts=={'users':65,'reviews':189,'products':135},counts
            row.update(counts=counts,allSelectedFieldsVerified=True)
    rows.append(row)
    print(json.dumps(row),flush=True)
(ROOT/'validation.json').write_text(json.dumps(rows,indent=2))
