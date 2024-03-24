# Resolve_Parallel_Entry_Resolver_Returns_Error_For_One_Service

## User Request

```graphql
{
  viewer {
    userId
    name
  }
}
```

## Result

```json
{
  "errors": [
    {
      "message": "Field \"viewer\" produced an error",
      "path": [
        "viewer"
      ],
      "extensions": {
        "remotePath": [
          "viewer",
          "name"
        ],
        "remoteLocations": [
          {
            "line": 1,
            "column": 24
          }
        ]
      }
    }
  ],
  "data": {
    "viewer": {
      "userId": "456",
      "name": null
    }
  }
}
```

## QueryPlan

```json
{
  "document": "{ viewer { userId name } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "a",
            "document": "query fetch_viewer_1 { viewer { name } }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "b",
            "document": "query fetch_viewer_2 { viewer { userId } }",
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

