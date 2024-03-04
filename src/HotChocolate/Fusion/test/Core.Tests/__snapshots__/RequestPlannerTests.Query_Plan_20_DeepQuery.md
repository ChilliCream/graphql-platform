# Query_Plan_20_DeepQuery

## UserRequest

```graphql
query GetUser {
  users {
    name
    reviews {
      body
      author {
        name
        birthdate
        reviews {
          body
          author {
            name
            birthdate
          }
        }
      }
    }
  }
}
```

## QueryPlan

```json
{
  "document": "query GetUser { users { name reviews { body author { name birthdate reviews { body author { name birthdate } } } } } }",
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
        "document": "query GetUser_2($__fusion_exports__1: [ID!]!) { nodes(ids: $__fusion_exports__1) { ... on User { reviews { body author { name reviews { body author { name __fusion_exports__2: id } } __fusion_exports__3: id } } __fusion_exports__1: id } } }",
        "selectionSetId": 1,
        "path": [
          "nodes"
        ],
        "requires": [
          {
            "variable": "__fusion_exports__1"
          }
        ],
        "provides": [
          {
            "variable": "__fusion_exports__2"
          },
          {
            "variable": "__fusion_exports__3"
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
            "document": "query GetUser_3($__fusion_exports__2: [ID!]!) { usersById(ids: $__fusion_exports__2) { birthdate __fusion_exports__2: id } }",
            "selectionSetId": 5,
            "path": [
              "usersById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__2"
              }
            ]
          },
          {
            "type": "ResolveByKeyBatch",
            "subgraph": "Accounts",
            "document": "query GetUser_4($__fusion_exports__3: [ID!]!) { usersById(ids: $__fusion_exports__3) { birthdate __fusion_exports__3: id } }",
            "selectionSetId": 3,
            "path": [
              "usersById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__3"
              }
            ]
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          3,
          5
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "User_id",
    "__fusion_exports__2": "User_id",
    "__fusion_exports__3": "User_id"
  }
}
```

