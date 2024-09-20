# Query_Plan_01

## UserRequest

```graphql
query GetUser {
  users {
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
  "document": "query GetUser { users { name reviews { body author { name } } } }",
  "operation": "GetUser",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Accounts",
        "document": "query GetUser_1 { users { name __fusion_exports__1: id } }",
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
        "type": "Compose",
        "selectionSetIds": [
          1
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "User_id"
  }
}
```

