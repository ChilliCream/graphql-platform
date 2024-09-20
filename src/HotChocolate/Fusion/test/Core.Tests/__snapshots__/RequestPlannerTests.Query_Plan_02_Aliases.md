# Query_Plan_02_Aliases

## UserRequest

```graphql
query GetUser {
  a: users {
    name
    reviews {
      body
      author {
        name
      }
    }
  }
  b: users {
    name
    reviews {
      body
      author {
        name
      }
    }
  }
}
```

## QueryPlan

```json
{
  "document": "query GetUser { a: users { name reviews { body author { name } } } b: users { name reviews { body author { name } } } }",
  "operation": "GetUser",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Accounts",
        "document": "query GetUser_1 { a: users { name __fusion_exports__1: id } b: users { name __fusion_exports__2: id } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__1"
          },
          {
            "variable": "__fusion_exports__2"
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
            "subgraph": "Reviews",
            "document": "query GetUser_2($__fusion_exports__1: [ID!]!) { nodes(ids: $__fusion_exports__1) { ... on User { reviews { body author { name } } __fusion_exports__1: id } } }",
            "selectionSetId": 1,
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
            "type": "ResolveByKeyBatch",
            "subgraph": "Reviews",
            "document": "query GetUser_3($__fusion_exports__2: [ID!]!) { nodes(ids: $__fusion_exports__2) { ... on User { reviews { body author { name } } __fusion_exports__2: id } } }",
            "selectionSetId": 2,
            "path": [
              "nodes"
            ],
            "requires": [
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
          1,
          2
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "User_id",
    "__fusion_exports__2": "User_id"
  }
}
```

