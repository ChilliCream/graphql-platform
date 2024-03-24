# Subgraph_Error_For_Field_Within_A_List

## User Request

```graphql
{
  userById(id: "VXNlcgppMQ==") {
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

## Result

```json
{
  "errors": [
    {
      "message": "SOME REVIEW ERROR",
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
        ],
        "remoteLocations": [
          {
            "line": 1,
            "column": 107
          }
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

## QueryPlan

```json
{
  "document": "{ userById(id: \u0022VXNlcgppMQ==\u0022) { account1: birthdate account2: birthdate username reviews { body errorField } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Accounts",
        "document": "query fetch_userById_1 { userById(id: \u0022VXNlcgppMQ==\u0022) { account1: birthdate account2: birthdate username __fusion_exports__1: id } }",
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

