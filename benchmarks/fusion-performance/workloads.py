import json
import pathlib

ROOT = pathlib.Path(__file__).resolve().parent
CASES = []


def add(name, query, kind, size, rate, time_unit="1s"):
    CASES.append(dict(name=name, query=query, variables={}, vus=4, kind=kind, size=size, rate=rate, timeUnit=time_unit))
    (ROOT / "requests").mkdir(exist_ok=True)
    (ROOT / "requests" / f"{name}.json").write_text(json.dumps(dict(query=query, variables={}), separators=(",", ":")))


for size, rate in [(1000, 100), (5000, 50), (20000, 20)]:
    add(f"list-{size}-plain", f"query List {{ shapeProducts(count:{size}) {{ upc name price weight }} }}", "plain", size, rate)
product = "inStock name price shippingEstimate upc weight"
user = "id username name"
review = "id body"
nested = f"""fragment User on User {{{user}}}
fragment Review on Review {{{review}}}
fragment Product on Product {{{product}}}
query Nested {{
 users {{ ...User reviews {{ ...Review product {{ ...Product reviews {{ ...Review author {{ ...User reviews {{ ...Review product {{ ...Product }} }} }} }} }} }} }}
 topProducts {{ ...Product reviews {{ ...Review author {{ ...User reviews {{ ...Review product {{ ...Product }} }} }} }} }}
}}"""
add("nested", nested, "nested", 1, 50)
fields = " ".join(f'... @defer(label:"d{i}"){{p{i}:shapeProducts(count:1){{upc name inStock shippingEstimate}}}}' for i in range(5))
add("defer-5", "query Deferred { __typename " + fields + "}", "defer", 5, 100)
(ROOT / "cases.json").write_text(json.dumps(CASES, indent=2))
