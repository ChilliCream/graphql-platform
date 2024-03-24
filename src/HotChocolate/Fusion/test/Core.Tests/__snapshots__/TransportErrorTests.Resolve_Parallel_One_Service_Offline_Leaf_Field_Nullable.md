# Resolve_Parallel_One_Service_Offline_Leaf_Field_Nullable

## User Request

```graphql
{
  viewer {
    user? {
      name?
    }
    latestReview {
      body
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
    "viewer": {
      "user": null,
      "latestReview": {
        "body": "Love it!"
      }
    }
  }
}
```

## QueryPlan

```json
{
  "document": "{ viewer { user? { name? } latestReview { body } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Accounts",
            "document": "query fetch_viewer_1 { viewer { user? { name? } } }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "Reviews2",
            "document": "query fetch_viewer_2 { viewer { latestReview { body } } }",
            "selectionSetId": 0
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      }
    ]
  }
}
```

