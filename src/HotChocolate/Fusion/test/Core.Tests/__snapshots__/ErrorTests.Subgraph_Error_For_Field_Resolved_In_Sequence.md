# Subgraph_Error_For_Field_Resolved_In_Sequence

## User Request

```graphql
{
  reviewById(id: "UmV2aWV3Cmkx") {
    body
    author {
      username
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
      "message": "SOME USER ERROR",
      "path": [
        "reviewById",
        "author",
        "errorField"
      ],
      "extensions": {
        "remotePath": [
          "userById",
          "errorField"
        ],
        "remoteLocations": [
          {
            "line": 1,
            "column": 101
          }
        ]
      }
    }
  ],
  "data": {
    "reviewById": {
      "body": "Love it!",
      "author": {
        "username": "@ada",
        "errorField": null
      }
    }
  }
}
```

## QueryPlan

```json
{
  "document": "{ reviewById(id: \u0022UmV2aWV3Cmkx\u0022) { body author { username errorField } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Reviews2",
        "document": "query fetch_reviewById_1 { reviewById(id: \u0022UmV2aWV3Cmkx\u0022) { body author { __fusion_exports__1: id } } }",
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
        "subgraph": "Accounts",
        "document": "query fetch_reviewById_2($__fusion_exports__1: ID!) { userById(id: $__fusion_exports__1) { username errorField } }",
        "selectionSetId": 2,
        "path": [
          "userById"
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

