# EntityResolver_Skip_On_SubField_Fragment_SubField_Selected_Separately

## Result

```json
{
  "data": {
    "productById": {
      "name": "string",
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
    name
    price
  }
}

fragment Test on Product {
  name
}
```

## QueryPlan Hash

```text
CE8F8A324258B4FB0800D2D67B1B7582688DA221
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { productById(id: \u00221\u0022) { ... Test @skip(if: $skip) name price } } fragment Test on Product { name }",
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

