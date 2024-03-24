# Resolve_Parallel_Both_Services_Offline_Shared_Entry_Field_Nullable

## User Request

```graphql
{
  viewer? {
    user {
      name
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
    },
    {
      "message": "Internal Execution Error"
    }
  ],
  "data": {
    "viewer": null
  }
}
```

## QueryPlan

```json
{
  "document": "{ viewer? { user { name } latestReview { body } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Accounts",
            "document": "query fetch_viewer_1 { viewer { user { name } } }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "Reviews2",
            "document": "query fetch_viewer_2 { viewer? { latestReview { body } } }",
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

