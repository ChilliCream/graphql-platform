# Query_Plan_19_Requires

## UserRequest

```graphql
query Requires {
  reviews {
    body
    author {
      name
      birthdate
    }
    product {
      name
      deliveryEstimate(zip: "12345") {
        min
        max
      }
    }
  }
}
```

## QueryPlan

```json
{
  "document": "query Requires { reviews { body author { name birthdate } product { name deliveryEstimate(zip: \u002212345\u0022) { min max } } } }",
  "operation": "Requires",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Reviews2",
        "document": "query Requires_1 { reviews { body author { name __fusion_exports__3: id } product { __fusion_exports__4: id } } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__3"
          },
          {
            "variable": "__fusion_exports__4"
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
            "type": "ResolveByKeyBatch",
            "subgraph": "Accounts",
            "document": "query Requires_2($__fusion_exports__3: [ID!]!) { usersById(ids: $__fusion_exports__3) { birthdate __fusion_exports__3: id } }",
            "selectionSetId": 2,
            "path": [
              "usersById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__3"
              }
            ]
          },
          {
            "type": "ResolveByKeyBatch",
            "subgraph": "Products",
            "document": "query Requires_4($__fusion_exports__4: [ID!]!) { nodes(ids: $__fusion_exports__4) { ... on Product { name __fusion_exports__1: dimension { size } __fusion_exports__2: dimension { weight } __fusion_exports__4: id } } }",
            "selectionSetId": 3,
            "path": [
              "nodes"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__4"
              }
            ],
            "provides": [
              {
                "variable": "__fusion_exports__1"
              },
              {
                "variable": "__fusion_exports__2"
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
      },
      {
        "type": "Resolve",
        "subgraph": "Shipping",
        "document": "query Requires_3($__fusion_exports__1: Int!, $__fusion_exports__2: Int!, $__fusion_exports__4: ID!) { productById(id: $__fusion_exports__4) { deliveryEstimate(size: $__fusion_exports__1, weight: $__fusion_exports__2, zip: \u002212345\u0022) { min max } } }",
        "selectionSetId": 3,
        "path": [
          "productById"
        ],
        "requires": [
          {
            "variable": "__fusion_exports__1"
          },
          {
            "variable": "__fusion_exports__2"
          },
          {
            "variable": "__fusion_exports__4"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          3
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "Product_dimension_size",
    "__fusion_exports__2": "Product_dimension_weight",
    "__fusion_exports__3": "User_id",
    "__fusion_exports__4": "Product_id"
  }
}
```

