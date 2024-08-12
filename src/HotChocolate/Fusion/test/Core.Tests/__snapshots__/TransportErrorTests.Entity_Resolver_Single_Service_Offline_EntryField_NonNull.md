# Entity_Resolver_Single_Service_Offline_EntryField_NonNull

## Result

```json
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 2,
          "column": 3
        }
      ],
      "path": [
        "productById"
      ],
      "extensions": {
        "code": "HC0018"
      }
    },
    {
      "message": "Internal Execution Error"
    }
  ],
  "data": null
}
```

## Request

```graphql
{
  productById(id: "1") {
    id
    name
    price
  }
}
```

## QueryPlan Hash

```text
3E29A9FA134FCCF20127189A9DE1B4CFB4492EAE
```

## QueryPlan

```json
{
  "document": "{ productById(id: \u00221\u0022) { id name price } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_productById_1 { productById(id: \u00221\u0022) { id name price } }",
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

