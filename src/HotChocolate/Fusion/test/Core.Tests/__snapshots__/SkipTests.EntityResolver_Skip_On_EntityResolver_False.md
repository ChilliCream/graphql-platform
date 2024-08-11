# EntityResolver_Skip_On_EntityResolver

## Result

```json
{
  "data": {
    "productById": {
      "id": "1",
      "name": "string"
    }
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  productById(id: "1") @skip(if: $skip) {
    id
    name
  }
}
```

## QueryPlan Hash

```text
120894E3876F3FBD797F056121AA37C7EB234DA8
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { productById(id: \u00221\u0022) @skip(if: $skip) { id name } }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query Test_1 { productById(id: \u00221\u0022) { id name } }",
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

