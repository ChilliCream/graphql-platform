# Field_Below_Shared_Field_Only_Available_On_One_Subgraph_Type_Of_Shared_Field_Not_Node_2

## Result

```json
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 5,
          "column": 7
        }
      ],
      "path": [
        "productById",
        "subgraph1Only",
        "sharedLinked"
      ],
      "extensions": {
        "code": "HC0018"
      }
    },
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 6,
          "column": 9
        }
      ],
      "path": [
        "productById",
        "subgraph1Only",
        "sharedLinked",
        "subgraph2Only"
      ],
      "extensions": {
        "code": "HC0018"
      }
    }
  ],
  "data": {
    "productById": {
      "subgraph1Only": null
    }
  }
}
```

## Request

```graphql
query($productId: ID!) {
  productById(id: $productId) {
    subgraph1Only {
      subgraph2Only
      sharedLinked {
        subgraph2Only
        sharedScalar
      }
    }
  }
}
```

## QueryPlan Hash

```text
16221984386FDA488A6F5EAB821D9A7090826999
```

## QueryPlan

```json
{
  "document": "query($productId: ID!) { productById(id: $productId) { subgraph1Only { subgraph2Only sharedLinked { subgraph2Only sharedScalar } } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_productById_1($productId: ID!) { productById(id: $productId) { subgraph1Only { sharedLinked { sharedScalar } __fusion_exports__1: id } } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__1"
          }
        ],
        "forwardedVariables": [
          {
            "variable": "productId"
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
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query fetch_productById_2($__fusion_exports__1: ID!) { productAvailabilityById(id: $__fusion_exports__1) { sharedLinked { subgraph2Only sharedScalar } } }",
            "selectionSetId": 2,
            "path": [
              "productAvailabilityById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__1"
              }
            ]
          },
          {
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query fetch_productById_3($__fusion_exports__1: ID!) { productAvailabilityById(id: $__fusion_exports__1) { subgraph2Only } }",
            "selectionSetId": 2,
            "path": [
              "productAvailabilityById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__1"
              }
            ]
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          2
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "ProductAvailability_id"
  }
}
```

