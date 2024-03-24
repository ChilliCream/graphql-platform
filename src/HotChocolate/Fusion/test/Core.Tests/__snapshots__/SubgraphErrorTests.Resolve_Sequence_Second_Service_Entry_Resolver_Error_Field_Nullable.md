# Resolve_Sequence_Second_Service_Entry_Resolver_Error_Field_Nullable

## User Request

```graphql
{
  productById(id: "1") {
    id
    name
    price
    score?
  }
}
```

## Result

```json
{
  "errors": [
    {
      "message": "Field \"productById\" produced an error",
      "path": [
        "productById",
        "score"
      ],
      "extensions": {
        "remotePath": [
          "productById"
        ],
        "remoteLocations": [
          {
            "line": 1,
            "column": 56
          }
        ]
      }
    }
  ],
  "data": {
    "productById": {
      "id": "456",
      "name": "string",
      "price": 123.456,
      "score": null
    }
  }
}
```

## QueryPlan

```json
{
  "document": "{ productById(id: \u00221\u0022) { id name price score? } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "a",
        "document": "query fetch_productById_1 { productById(id: \u00221\u0022) { id name price __fusion_exports__1: id } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__1"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      },
      {
        "type": "Resolve",
        "subgraph": "b",
        "document": "query fetch_productById_2($__fusion_exports__1: ID!) { productById(id: $__fusion_exports__1) { score? } }",
        "selectionSetId": 1,
        "path": [
          "productById"
        ],
        "requires": [
          {
            "variable": "__fusion_exports__1"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          1
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "Product_id"
  }
}
```

