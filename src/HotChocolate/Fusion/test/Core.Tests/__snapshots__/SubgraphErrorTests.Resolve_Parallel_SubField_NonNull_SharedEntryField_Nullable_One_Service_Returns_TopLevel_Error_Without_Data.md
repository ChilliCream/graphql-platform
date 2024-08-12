# Resolve_Parallel_SubField_NonNull_SharedEntryField_Nullable_One_Service_Returns_TopLevel_Error_Without_Data

## Result

```json
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "locations": [
        {
          "line": 3,
          "column": 5
        }
      ],
      "path": [
        "viewer",
        "userId"
      ],
      "extensions": {
        "code": "HC0018"
      }
    },
    {
      "message": "Top Level Error"
    }
  ],
  "data": {
    "viewer": null
  }
}
```

## Request

```graphql
{
  viewer {
    userId
    name
  }
}
```

## QueryPlan Hash

```text
0728EE40A767B43E14FF62896779067DFF1C53FF
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
            "subgraph": "Subgraph_1",
            "document": "query fetch_viewer_1 { viewer { name } }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "Subgraph_2",
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

