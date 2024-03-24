# Resolve_Service_Offline_Entry_Field_NonNull

## User Request

```graphql
{
  reviewById(id: "UmV2aWV3Cmkx")! {
    body
  }
}
```

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
        "reviewById"
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

## QueryPlan

```json
{
  "document": "{ reviewById(id: \u0022UmV2aWV3Cmkx\u0022)! { body } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Reviews2",
        "document": "query fetch_reviewById_1 { reviewById(id: \u0022UmV2aWV3Cmkx\u0022) { body } }",
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

