# Authors_And_Reviews_Batch_Requests

## Result

```json
{
  "data": {
    "reviews": [
      {
        "body": "Love it!",
        "author": {
          "birthdate": "1815-12-10"
        }
      },
      {
        "body": "Too expensive.",
        "author": {
          "birthdate": "1912-06-23"
        }
      },
      {
        "body": "Could be better.",
        "author": {
          "birthdate": "1815-12-10"
        }
      },
      {
        "body": "Prefer something else.",
        "author": {
          "birthdate": "1912-06-23"
        }
      }
    ]
  }
}
```

## Request

```graphql
query GetUser {
  reviews {
    body
    author {
      birthdate
    }
  }
}
```

## QueryPlan Hash

```text
333DF30FB036FBB1BEB270E16C4A7D635684E74A
```

## QueryPlan

```json
{
  "document": "query GetUser { reviews { body author { birthdate } } }",
  "operation": "GetUser",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Reviews2",
        "document": "query GetUser_1 { reviews { body author { __fusion_exports__1: id } } }",
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
        "subgraph": "Accounts",
        "document": "query GetUser_2($__fusion_exports__1: [ID!]!) { usersById(ids: $__fusion_exports__1) { birthdate __fusion_exports__1: id } }",
        "selectionSetId": 2,
        "path": [
          "usersById"
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
    "__fusion_exports__1": "User_id"
  }
}
```

