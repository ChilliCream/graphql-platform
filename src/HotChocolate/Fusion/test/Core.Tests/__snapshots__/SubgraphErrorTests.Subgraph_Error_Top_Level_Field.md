# Subgraph_Error_Top_Level_Field

## User Request

```graphql
{
  field
}
```

## Result

```json
{
  "errors": [
    {
      "message": "Field \"field\" produced an error",
      "path": [
        "field"
      ],
      "extensions": {
        "remotePath": [
          "field"
        ],
        "remoteLocations": [
          {
            "line": 1,
            "column": 23
          }
        ]
      }
    }
  ],
  "data": {
    "field": null
  }
}
```

## QueryPlan

```json
{
  "document": "{ field }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "a",
        "document": "query fetch_field_1 { field }",
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

