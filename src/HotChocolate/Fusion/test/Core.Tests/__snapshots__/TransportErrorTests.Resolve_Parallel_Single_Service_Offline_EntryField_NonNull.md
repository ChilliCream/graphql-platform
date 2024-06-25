# Resolve_Parallel_Single_Service_Offline_EntryField_NonNull

## Result

```json
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 2,
          "column": 3
        }
      ],
      "path": [
        "viewer"
      ],
      "extensions": {
        "code": "HC0018"
      }
    },
    {
      "message": "Internal Execution Error"
    }
  ],
  "data": null
}
```

## Request

```graphql
{
  viewer {
    name
  }
}
```

## QueryPlan Hash

```text
8B6791F7C91D1B779FF099AD8C7FC0D5980195EF
```

## QueryPlan

```json
{
  "document": "{ viewer { name } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_viewer_1 { viewer { name } }",
        "selectionSetId": 0
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

