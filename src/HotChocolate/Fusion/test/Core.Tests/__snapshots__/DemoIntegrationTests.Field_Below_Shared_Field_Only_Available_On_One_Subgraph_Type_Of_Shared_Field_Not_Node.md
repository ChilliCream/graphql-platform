# Field_Below_Shared_Field_Only_Available_On_One_Subgraph_Type_Of_Shared_Field_Not_Node

## Result

```json
{
  "data": {
    "productById": {
      "availability": {
        "mail": {
          "canOnlyBeDeliveredToCurb": true
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
      mail {
        canOnlyBeDeliveredToCurb
      }
    }
  }
}
```

## QueryPlan Hash

```text
8935DF07FF6B056F1D082CD9DA1EC6CEEEC91818
```

## QueryPlan

```json
{
  "document": "query($productId: ID!) { productById(id: $productId) { availability { mail { canOnlyBeDeliveredToCurb } } } }",
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
        "document": "query fetch_productById_2($__fusion_exports__1: ID!) { node(id: $__fusion_exports__1) { ... on ProductAvailability { mail { canOnlyBeDeliveredToCurb } } } }",
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

