# Resolve_ResolveByKey_Sequence_Second_Service_Offline_Leaf_Field_Nullable

## User Request

```graphql
{
  reviews {
    body
    author? {
      birthdate?
    }
  }
}
```

## Result

```json
{
  "errors": [
    {
      "message": "Internal Execution Error"
    }
  ],
  "data": {
    "reviews": [
      {
        "body": "Love it!",
        "author": null
      },
      {
        "body": "Too expensive.",
        "author": null
      },
      {
        "body": "Could be better.",
        "author": null
      },
      {
        "body": "Prefer something else.",
        "author": null
      }
    ]
  }
}
```

## QueryPlan

```json
{
  "document": "{ reviews { body author? { birthdate? } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Reviews",
        "document": "query fetch_reviews_1 { reviews { body author? { __fusion_exports__1: id } } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__1"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [0]
      },
      {
        "type": "ResolveByKeyBatch",
        "subgraph": "Accounts",
        "document": "query fetch_reviews_2($__fusion_exports__1: [ID!]!) { usersById(ids: $__fusion_exports__1) { birthdate? __fusion_exports__1: id } }",
        "selectionSetId": 2,
        "path": ["usersById"],
        "requires": [
          {
            "variable": "__fusion_exports__1"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [2]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "User_id"
  }
}
```
