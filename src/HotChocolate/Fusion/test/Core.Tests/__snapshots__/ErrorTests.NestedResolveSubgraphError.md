# NestedResolveSubgraphError

## Result

```json
{
  "errors": [
    {
      "message": "SOME USER ERROR",
      "locations": [
        {
          "line": 1,
          "column": 101
        }
      ],
      "path": [
        "reviewById",
        "author",
        "errorField"
      ],
      "extensions": {
        "remotePath": [
          "userById",
          "errorField"
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

## Request

```graphql
{
  reviewById(id: "UmV2aWV3OjE=") {
    body
    author {
      username
      errorField
    }
  }
}
```

## QueryPlan Hash

```text
7F7E32C4C0C896F19A72BEE33ED9FFBD051C91E6
```

## QueryPlan

```json
{
  "document": "{ reviewById(id: \u0022UmV2aWV3OjE=\u0022) { body author { username errorField } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Reviews2",
        "document": "query fetch_reviewById_1 { reviewById(id: \u0022UmV2aWV3OjE=\u0022) { body author { __fusion_exports__1: id } } }",
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

