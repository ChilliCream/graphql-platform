# Subgraph_Error_For_Field_Resolved_Via_Key_Batch

## User Request

```graphql
{
  reviews {
    body
    author {
      id
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
        "reviews",
        0,
        "author",
        "errorField"
      ],
      "extensions": {
        "remotePath": [
          "usersById",1,
          "errorField"
        ],
        "remoteLocations": [
          {
            "line": 1,
            "column": 94
          }
        ]
      }
    },
    {
      "message": "SOME USER ERROR",
      "path": [
        "reviews",
        0,
        "author",
        "errorField"
      ],
      "extensions": {
        "remotePath": [
          "usersById",0,
          "errorField"
        ],
        "remoteLocations": [
          {
            "line": 1,
            "column": 94
          }
        ]
      }
    }
  ],
  "data": {
    "reviews": [
      {
        "body": "Love it!",
        "author": {
          "id": "VXNlcgppMQ==",
          "errorField": null
        }
      },
      {
        "body": "Too expensive.",
        "author": {
          "id": "VXNlcgppMg==",
          "errorField": null
        }
      },
      {
        "body": "Could be better.",
        "author": {
          "id": "VXNlcgppMQ==",
          "errorField": null
        }
      },
      {
        "body": "Prefer something else.",
        "author": {
          "id": "VXNlcgppMg==",
          "errorField": null
        }
      }
    ]
  }
}
```

## QueryPlan

```json
{
  "document": "{ reviews { body author { id errorField } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Reviews2",
        "document": "query fetch_reviews_1 { reviews { body author { id __fusion_exports__1: id } } }",
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
        "document": "query fetch_reviews_2($__fusion_exports__1: [ID!]!) { usersById(ids: $__fusion_exports__1) { errorField __fusion_exports__1: id } }",
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

