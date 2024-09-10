# Authors_And_Reviews_Query_GetUserReviews

## Result

```json
{
  "data": {
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
8F6E2CCB58DA60498BA9F134BF2F1B8D5C24FBA0
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
        "subgraph": "Reviews2",
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

