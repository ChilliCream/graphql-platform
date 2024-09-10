# ResolveByKey_Handles_Null_Item_Correctly

## Result

```json
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 9,
          "column": 13
        }
      ],
      "path": [
        "viewer",
        "recommendedResalableProducts",
        "edges",
        1,
        "node",
        "product",
        "name"
      ],
      "extensions": {
        "code": "HC0018"
      }
    }
  ],
  "data": {
    "viewer": {
      "recommendedResalableProducts": {
        "edges": [
          {
            "node": {
              "product": {
                "id": "UHJvZHVjdDox",
                "name": "Table"
              }
            }
          },
          {
            "node": {
              "product": null
            }
          },
          {
            "node": {
              "product": {
                "id": "UHJvZHVjdDoz",
                "name": "Chair"
              }
            }
          }
        ]
      }
    }
  }
}
```

## Request

```graphql
{
  viewer {
    recommendedResalableProducts {
      edges {
        node {
          product? {
            id
            name
          }
        }
      }
    }
  }
}
```

## QueryPlan Hash

```text
143888B7D930092D26863B1CCBEFA6A7A06739E8
```

## QueryPlan

```json
{
  "document": "{ viewer { recommendedResalableProducts { edges { node { product? { id name } } } } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Resale",
        "document": "query fetch_viewer_1 { viewer { recommendedResalableProducts { edges { node { product? { id __fusion_exports__1: id } } } } } }",
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
        "type": "ResolveByKeyBatch",
        "subgraph": "Products",
        "document": "query fetch_viewer_2($__fusion_exports__1: [ID!]!) { nodes(ids: $__fusion_exports__1) { ... on Product { name __fusion_exports__1: id } } }",
        "selectionSetId": 5,
        "path": [
          "nodes"
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
          5
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "Product_id"
  }
}
```

