# Authors_And_Reviews_Query_GetUserReviews_Skip_Author_ErrorField

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
              "name": "@ada",
              "birthdate": "1815-12-10"
            }
          },
          {
            "body": "Could be better.",
            "author": {
              "name": "@ada",
              "birthdate": "1815-12-10"
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
              "name": "@alan",
              "birthdate": "1912-06-23"
            }
          },
          {
            "body": "Prefer something else.",
            "author": {
              "name": "@alan",
              "birthdate": "1912-06-23"
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
query GetUser($skip: Boolean!) {
  users {
    name
    reviews {
      body
      author {
        name
        birthdate
        errorField @skip(if: $skip)
      }
    }
  }
}
```

## QueryPlan Hash

```text
D0E7D1201A7F70F2FE96F5DCD47EFD344C05E88F
```

## QueryPlan

```json
{
  "document": "query GetUser($skip: Boolean!) { users { name reviews { body author { name birthdate errorField @skip(if: $skip) } } } }",
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
        "document": "query GetUser_2($__fusion_exports__1: [ID!]!) { nodes(ids: $__fusion_exports__1) { ... on User { reviews { body author { name __fusion_exports__2: id } } __fusion_exports__1: id } } }",
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
        "type": "ResolveByKeyBatch",
        "subgraph": "Accounts",
        "document": "query GetUser_3($__fusion_exports__2: [ID!]!, $skip: Boolean!) { usersById(ids: $__fusion_exports__2) { birthdate errorField @skip(if: $skip) __fusion_exports__2: id } }",
        "selectionSetId": 3,
        "path": [
          "usersById"
        ],
        "requires": [
          {
            "variable": "__fusion_exports__2"
          }
        ],
        "forwardedVariables": [
          {
            "variable": "skip"
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
    "__fusion_exports__1": "User_id",
    "__fusion_exports__2": "User_id"
  }
}
```

