# Field_Below_Shared_Field_Only_Available_On_One_Subgraph_Type_Of_Shared_Field_Not_Node_2

## Result

```json
{
  "data": {
    "productById": {
      "subgraph1Only": {
        "subgraph2Only": true,
        "sharedLinked": {
          "subgraph2Only": true,
          "sharedScalar": "string"
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
        "type": "Resolve",
        "subgraph": "Subgraph_2",
        "document": "query fetch_productById_2($__fusion_exports__1: ID!) { productAvailabilityById(id: $__fusion_exports__1) { subgraph2Only sharedLinked { subgraph2Only } } }",
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
