# Entity_Resolver_First_Service_Offline_SubFields_NonNull_EntryField_NonNull

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
    score
  }
}
```

## QueryPlan Hash

```text
A5FE502D9F6F0548B898BC17A33BC0F2A2A13AE6
```

## QueryPlan

```json
{
  "document": "{ productById(id: \u00221\u0022) { id name price score } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
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
        "subgraph": "Subgraph_2",
        "document": "query fetch_productById_2($__fusion_exports__1: ID!) { productById(id: $__fusion_exports__1) { score } }",
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

