# Resolve_Sequence_Second_Service_Entry_Resolver_Returns_Null_Field_NonNull

## User Request

```graphql
{
  productById(id: "1") {
    id
    name
    price
    score!
  }
}
```

## Result

```json
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 6,
          "column": 5
        }
      ],
      "path": [
        "productById",
        "score"
      ],
      "extensions": {
        "code": "HC0018"
      }
    }
  ],
  "data": {
    "productById": null
  }
}
```

## QueryPlan

```json
{
  "document": "{ productById(id: \u00221\u0022) { id name price score! } }",
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
        "document": "query fetch_productById_2($__fusion_exports__1: ID!) { productById(id: $__fusion_exports__1) { score! } }",
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

