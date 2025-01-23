# Field_Below_Shared_Field_Only_Available_On_One_Subgraph_Type_Of_Shared_Field_Not_Node_2

## Result

```json
{
  "data": {
    "productById": {
      "availability": {
        "isFutureRelease": true,
        "mail": {
          "canOnlyBeDeliveredToCurb": true,
          "classification": "string"
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
    availability {
      isFutureRelease
      mail {
        canOnlyBeDeliveredToCurb
        classification
      }
    }
  }
}
```

## QueryPlan Hash

```text
31CD4258CC2DACAA6791D01B537D990C3EB60E68
```

## QueryPlan

```json
{
  "document": "query($productId: ID!) { productById(id: $productId) { availability { isFutureRelease mail { canOnlyBeDeliveredToCurb classification } } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_2",
        "document": "query fetch_productById_1($productId: ID!) { productById(id: $productId) { availability { __fusion_exports__1: id } } }",
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
        "subgraph": "Subgraph_1",
        "document": "query fetch_productById_2($__fusion_exports__1: ID!) { node(id: $__fusion_exports__1) { ... on ProductAvailability { isFutureRelease mail { canOnlyBeDeliveredToCurb classification } } } }",
        "selectionSetId": 2,
        "path": [
          "node"
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

