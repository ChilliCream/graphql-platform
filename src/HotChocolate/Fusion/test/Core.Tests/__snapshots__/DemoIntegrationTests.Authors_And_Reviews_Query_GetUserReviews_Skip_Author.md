# Authors_And_Reviews_Query_GetUserReviews_Skip_Author

## Result

```json
{
  "data": {
    "users": [
      {
        "name": "Ada Lovelace",
        "reviews": [
          {
            "body": "Love it!"
          },
          {
            "body": "Could be better."
          }
        ]
      },
      {
        "name": "Alan Turing",
        "reviews": [
          {
            "body": "Too expensive."
          },
          {
            "body": "Prefer something else."
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
      author @skip(if: $skip) {
        name
        birthdate
      }
    }
  }
}
```

## QueryPlan Hash

```text
B1F750EE35FB23347DEC15FF5A9391D5EF5DB5EE
```

## QueryPlan

```json
{
  "document": "query GetUser($skip: Boolean!) { users { name reviews { body author @skip(if: $skip) { name birthdate } } } }",
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
        "document": "query GetUser_2($__fusion_exports__1: [ID!]!, $skip: Boolean!) { nodes(ids: $__fusion_exports__1) { ... on User { reviews { body author @skip(if: $skip) { name __fusion_exports__2: id } } __fusion_exports__1: id } } }",
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
          1
        ]
      },
      {
        "type": "ResolveByKeyBatch",
        "subgraph": "Accounts",
        "document": "query GetUser_3($__fusion_exports__2: [ID!]!) { usersById(ids: $__fusion_exports__2) { birthdate __fusion_exports__2: id } }",
        "selectionSetId": 3,
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

