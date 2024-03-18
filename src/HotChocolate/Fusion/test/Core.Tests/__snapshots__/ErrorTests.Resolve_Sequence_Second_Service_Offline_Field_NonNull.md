# Resolve_Sequence_Second_Service_Offline_Field_NonNull

## User Request

```graphql
{
  reviewById(id: "UmV2aWV3Cmkx") {
    body
    author! {
      username
    }
  }
}
```

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Subgraph Failure",
      "path": [
        "reviewById",
        "author"
      ]
    }
  ],
  "data": {
    "reviewById": null
  }
}
```

## QueryPlan

```json
{
  "document": "{ reviewById(id: \u0022UmV2aWV3Cmkx\u0022) { body author! { username } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Reviews2",
        "document": "query fetch_reviewById_1 { reviewById(id: \u0022UmV2aWV3Cmkx\u0022) { body author! { __fusion_exports__1: id } } }",
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
        "document": "query fetch_reviewById_2($__fusion_exports__1: ID!) { userById(id: $__fusion_exports__1) { username } }",
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

