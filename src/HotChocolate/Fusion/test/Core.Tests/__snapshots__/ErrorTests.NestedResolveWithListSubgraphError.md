# NestedResolveWithListSubgraphError

## Result

```json
{
  "errors": [
    {
      "message": "SOME REVIEW ERROR",
      "locations": [
        {
          "line": 1,
          "column": 107
        }
      ],
      "path": [
        "userById",
        "reviews",
        1,
        "errorField"
      ],
      "extensions": {
        "remotePath": [
          "authorById",
          "reviews",1,
          "errorField"
        ]
      }
    }
  ],
  "data": {
    "userById": {
      "account1": "1815-12-10",
      "account2": "1815-12-10",
      "username": "@ada",
      "reviews": [
        {
          "body": "Love it!",
          "errorField": null
        },
        {
          "body": "Could be better.",
          "errorField": null
        }
      ]
    }
  }
}
```

## Request

```graphql
{
  userById(id: "VXNlcjox") {
    account1: birthdate
    account2: birthdate
    username
    reviews {
      body
      errorField
    }
  }
}
```

## QueryPlan Hash

```text
D2A76543F95E65F1ADD5BD9E8ADB7A4A506757E9
```

## QueryPlan

```json
{
  "document": "{ userById(id: \u0022VXNlcjox\u0022) { account1: birthdate account2: birthdate username reviews { body errorField } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Accounts",
        "document": "query fetch_userById_1 { userById(id: \u0022VXNlcjox\u0022) { account1: birthdate account2: birthdate username __fusion_exports__1: id } }",
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
        "type": "Resolve",
        "subgraph": "Reviews2",
        "document": "query fetch_userById_2($__fusion_exports__1: ID!) { authorById(id: $__fusion_exports__1) { reviews { body errorField } } }",
        "selectionSetId": 1,
        "path": [
          "authorById"
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

