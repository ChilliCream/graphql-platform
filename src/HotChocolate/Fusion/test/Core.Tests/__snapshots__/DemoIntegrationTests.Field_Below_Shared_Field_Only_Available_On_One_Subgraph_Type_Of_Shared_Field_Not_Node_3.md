# Field_Below_Shared_Field_Only_Available_On_One_Subgraph_Type_Of_Shared_Field_Not_Node_3

## Result

```json
{
  "errors": [
    {
      "message": "The following variables were not declared: productId.",
      "locations": [
        {
          "line": 1,
          "column": 1
        }
      ],
      "extensions": {
        "specifiedBy": "https://spec.graphql.org/October2021/#sec-All-Variable-Uses-Defined"
      }
    },
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 6,
          "column": 7
        }
      ],
      "path": [
        "productById",
        "availability",
        "mail"
      ],
      "extensions": {
        "code": "HC0018"
      }
    },
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 9,
          "column": 9
        }
      ],
      "path": [
        "productById",
        "availability",
        "mail",
        "other"
      ],
      "extensions": {
        "code": "HC0018"
      }
    }
  ],
  "data": {
    "productById": {
      "availability": null
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
      isPastReleaseDate
      mail {
        canOnlyBeDeliveredToCurb
        classification
        other
      }
    }
  }
}
```

## QueryPlan Hash

```text
968025A3ACCA82E4E0A454346311F057E257CAEA
```

## QueryPlan

```json
{
  "document": "query($productId: ID!) { productById(id: $productId) { availability { isFutureRelease isPastReleaseDate mail { canOnlyBeDeliveredToCurb classification other } } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query fetch_productById_1($productId: ID!) { productById(id: $productId) { availability { isPastReleaseDate __fusion_exports__1: id } } }",
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
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query fetch_productById_3 { productById(id: $productId) { availability { mail { other } } } }",
            "selectionSetId": 0
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

