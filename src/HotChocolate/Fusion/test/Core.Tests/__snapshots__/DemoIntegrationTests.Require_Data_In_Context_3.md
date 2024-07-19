# Require_Data_In_Context_3

## Result

```json
{
  "data": {
    "users": [
      {
        "id": "VXNlcjox",
        "name": "Ada Lovelace",
        "birthdate": "1815-12-10",
        "reviews": [
          {
            "body": "Love it!",
            "author": {
              "name": "@ada",
              "birthdate": "1815-12-10"
            },
            "product": {
              "id": "UHJvZHVjdDox",
              "name": "Table",
              "deliveryEstimate": {
                "max": 800
              }
            }
          },
          {
            "body": "Could be better.",
            "author": {
              "name": "@ada",
              "birthdate": "1815-12-10"
            },
            "product": {
              "id": "UHJvZHVjdDoz",
              "name": "Chair",
              "deliveryEstimate": {
                "max": 90
              }
            }
          }
        ]
      },
      {
        "id": "VXNlcjoy",
        "name": "Alan Turing",
        "birthdate": "1912-06-23",
        "reviews": [
          {
            "body": "Too expensive.",
            "author": {
              "name": "@alan",
              "birthdate": "1912-06-23"
            },
            "product": {
              "id": "UHJvZHVjdDoy",
              "name": "Couch",
              "deliveryEstimate": {
                "max": 5300
              }
            }
          },
          {
            "body": "Prefer something else.",
            "author": {
              "name": "@alan",
              "birthdate": "1912-06-23"
            },
            "product": {
              "id": "UHJvZHVjdDox",
              "name": "Table",
              "deliveryEstimate": {
                "max": 800
              }
            }
          }
        ]
      }
    ]
  }
}
```

## Request

```graphql
query Large {
  users {
    id
    name
    birthdate
    reviews {
      body
      author {
        name
        birthdate
      }
      product {
        id
        name
        deliveryEstimate(zip: "abc") {
          max
        }
      }
    }
  }
}
```

## QueryPlan Hash

```text
B3E0703565E9626DBFAFDCB4CBD1DE815EBE4DB0
```

## QueryPlan

```json
{
  "document": "query Large { users { id name birthdate reviews { body author { name birthdate } product { id name deliveryEstimate(zip: \u0022abc\u0022) { max } } } } }",
  "operation": "Large",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Accounts",
        "document": "query Large_1 { users { id name birthdate __fusion_exports__3: id } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__3"
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
        "subgraph": "Reviews2",
        "document": "query Large_2($__fusion_exports__3: [ID!]!) { nodes(ids: $__fusion_exports__3) { ... on User { reviews { body author { name __fusion_exports__4: id } product { id __fusion_exports__6: id } } __fusion_exports__3: id } } }",
        "selectionSetId": 1,
        "path": [
          "nodes"
        ],
        "requires": [
          {
            "variable": "__fusion_exports__3"
          }
        ],
        "provides": [
          {
            "variable": "__fusion_exports__4"
          },
          {
            "variable": "__fusion_exports__6"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          1
        ]
      },
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "ResolveByKeyBatch",
            "subgraph": "Accounts",
            "document": "query Large_3($__fusion_exports__4: [ID!]!) { usersById(ids: $__fusion_exports__4) { birthdate __fusion_exports__4: id } }",
            "selectionSetId": 3,
            "path": [
              "usersById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__4"
              }
            ]
          },
          {
            "type": "ResolveByKeyBatch",
            "subgraph": "Products",
            "document": "query Large_5($__fusion_exports__6: [ID!]!) { nodes(ids: $__fusion_exports__6) { ... on Product { name __fusion_exports__1: dimension { size } __fusion_exports__2: dimension { weight } __fusion_exports__5: id __fusion_exports__6: id } } }",
            "selectionSetId": 4,
            "path": [
              "nodes"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__6"
              }
            ],
            "provides": [
              {
                "variable": "__fusion_exports__1"
              },
              {
                "variable": "__fusion_exports__2"
              },
              {
                "variable": "__fusion_exports__5"
              }
            ]
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          3
        ]
      },
      {
        "type": "Resolve",
        "subgraph": "Shipping",
        "document": "query Large_4($__fusion_exports__1: Int!, $__fusion_exports__2: Int!, $__fusion_exports__5: ID!) { productById(id: $__fusion_exports__5) { deliveryEstimate(size: $__fusion_exports__1, weight: $__fusion_exports__2, zip: \u0022abc\u0022) { max } } }",
        "selectionSetId": 4,
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
            "variable": "__fusion_exports__5"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          4
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "Product_dimension_size",
    "__fusion_exports__2": "Product_dimension_weight",
    "__fusion_exports__3": "User_id",
    "__fusion_exports__4": "User_id",
    "__fusion_exports__5": "Product_id",
    "__fusion_exports__6": "Product_id"
  }
}
```

