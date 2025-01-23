# Field_Below_Shared_Field_Only_Available_On_One_Subgraph_Type_Of_Shared_Field_Not_Node_2

## Result

```json
{
  "errors": [
    {
      "message": "The field `productById` does not exist on the type `Query`.",
      "locations": [
        {
          "line": 1,
          "column": 29
        }
      ],
      "extensions": {
        "type": "Query",
        "field": "productById",
        "responseName": "productById",
        "specifiedBy": "https://spec.graphql.org/October2021/#sec-Field-Selections-on-Objects-Interfaces-and-Unions-Types"
      }
    },
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
          "line": 6,
          "column": 9
        }
      ],
      "path": [
        "productById",
        "availability",
        "mail",
        "canOnlyBeDeliveredToCurb"
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
0AB239A55BF1E119982EB36A09571D08B8BA2C3D
```

## QueryPlan

```json
{
  "document": "query($productId: ID!) { productById(id: $productId) { availability { isFutureRelease mail { canOnlyBeDeliveredToCurb classification } } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query fetch_productById_1($productId: ID!) { productById(id: $productId) { availability { mail { classification } __fusion_exports__1: id } } }",
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
            "subgraph": "Subgraph_1",
            "document": "query fetch_productById_2 { productById(id: $productId) { availability { mail { canOnlyBeDeliveredToCurb } } } }",
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
        "document": "query fetch_productById_3($__fusion_exports__1: ID!) { node(id: $__fusion_exports__1) { ... on ProductAvailability { isFutureRelease } } }",
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

