# Field_Below_Shared_Field_Only_Available_On_One_Subgraph_Type_Of_Shared_Field_Not_Node

## Result

```json
{
  "data": {
    "productById": {
      "subgraph1Only": {
        "sharedLinked": {
          "subgraph2Only": true
        }
      }
    }
  }
}
```

## Request

```graphql
query($productId: ID!) {
  productById(id: $productId) {
    subgraph1Only {
      sharedLinked {
        subgraph2Only
      }
    }
  }
}
```

## QueryPlan Hash

```text
248A8F4DE404D0D21F7ABBC9255D2880DFDF4C2C
```

## QueryPlan

```json
{
  "document": "query($productId: ID!) { productById(id: $productId) { subgraph1Only { sharedLinked { subgraph2Only } } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_productById_1($productId: ID!) { productById(id: $productId) { subgraph1Only { __fusion_exports__1: id } } }",
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
        "type": "Resolve",
        "subgraph": "Subgraph_2",
        "document": "query fetch_productById_2($__fusion_exports__1: ID!) { productAvailabilityById(id: $__fusion_exports__1) { sharedLinked { subgraph2Only } } }",
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

