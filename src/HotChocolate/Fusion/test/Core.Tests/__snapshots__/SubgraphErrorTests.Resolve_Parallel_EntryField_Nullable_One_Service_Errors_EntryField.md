# Resolve_Parallel_EntryField_Nullable_One_Service_Errors_EntryField

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "locations": [
        {
          "line": 5,
          "column": 3
        }
      ],
      "path": [
        "other"
      ]
    }
  ],
  "data": {
    "viewer": {
      "name": "string"
    },
    "other": null
  }
}
```

## Request

```graphql
{
  viewer {
    name
  }
  other {
    userId
  }
}
```

## QueryPlan Hash

```text
1E9F0B5070B0EB2A79CBF03CDCC94C574189F814
```

## QueryPlan

```json
{
  "document": "{ viewer { name } other { userId } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Subgraph_1",
            "document": "query fetch_viewer_other_1 { viewer { name } }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query fetch_viewer_other_2 { other { userId } }",
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

