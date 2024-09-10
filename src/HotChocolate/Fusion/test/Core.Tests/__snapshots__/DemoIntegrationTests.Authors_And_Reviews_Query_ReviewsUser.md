# Authors_And_Reviews_Query_ReviewsUser

## Result

```json
{
  "data": {
    "a": [
      {
        "body": "Love it!",
        "author": {
          "name": "@ada"
        }
      },
      {
        "body": "Too expensive.",
        "author": {
          "name": "@alan"
        }
      },
      {
        "body": "Could be better.",
        "author": {
          "name": "@ada"
        }
      },
      {
        "body": "Prefer something else.",
        "author": {
          "name": "@alan"
        }
      }
    ],
    "b": [
      {
        "body": "Love it!",
        "author": {
          "name": "@ada"
        }
      },
      {
        "body": "Too expensive.",
        "author": {
          "name": "@alan"
        }
      },
      {
        "body": "Could be better.",
        "author": {
          "name": "@ada"
        }
      },
      {
        "body": "Prefer something else.",
        "author": {
          "name": "@alan"
        }
      }
    ],
    "users": [
      {
        "name": "Ada Lovelace",
        "reviews": [
          {
            "body": "Love it!",
            "author": {
              "name": "@ada"
            }
          },
          {
            "body": "Could be better.",
            "author": {
              "name": "@ada"
            }
          }
        ]
      },
      {
        "name": "Alan Turing",
        "reviews": [
          {
            "body": "Too expensive.",
            "author": {
              "name": "@alan"
            }
          },
          {
            "body": "Prefer something else.",
            "author": {
              "name": "@alan"
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
query GetUser {
  a: reviews {
    body
    author {
      name
    }
  }
  b: reviews {
    body
    author {
      name
    }
  }
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

## QueryPlan Hash

```text
3F11BECD0B3BF1E1BE77C2B13D1A1FAD1FFB5DC0
```

## QueryPlan

```json
{
  "document": "query GetUser { a: reviews { body author { name } } b: reviews { body author { name } } users { name reviews { body author { name } } } }",
  "operation": "GetUser",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Reviews2",
            "document": "query GetUser_1 { a: reviews { body author { name } } b: reviews { body author { name } } }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "Accounts",
            "document": "query GetUser_2 { users { name __fusion_exports__1: id } }",
            "selectionSetId": 0,
            "provides": [
              {
                "variable": "__fusion_exports__1"
              }
            ]
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
        "document": "query GetUser_3($__fusion_exports__1: [ID!]!) { nodes(ids: $__fusion_exports__1) { ... on User { reviews { body author { name } } __fusion_exports__1: id } } }",
        "selectionSetId": 3,
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
          3
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "User_id"
  }
}
```

