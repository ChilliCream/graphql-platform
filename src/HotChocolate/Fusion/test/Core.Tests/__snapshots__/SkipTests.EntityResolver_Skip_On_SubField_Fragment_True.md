# EntityResolver_Skip_On_SubField_Fragment

## Result

```json
{
  "data": {
    "productById": {
      "price": 123.456
    }
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  productById(id: "1") {
    ... Test @skip(if: $skip)
    price
  }
}

fragment Test on Product {
  name
}
```

## QueryPlan Hash

```text
F70E2C57BE7C90427FF9E449DE7F1251066313CE
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { productById(id: \u00221\u0022) { ... Test @skip(if: $skip) price } } fragment Test on Product { name }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query Test_1 { productById(id: \u00221\u0022) { name price } }",
        "selectionSetId": 0
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      }
    ]
  }
}
```

