# Resolve_Parallel_Nested_Object_Field_Of_One_Service_Returns_Error

## User Request

```graphql
{
  viewer {
    userId
    name
    obj {
      aField
      bField
    }
  }
}
```

## Result

```json
{
    "errors": [
    {
      "message": "Field \"obj\" produced an error",
      "path": [
        "viewer",
        "obj",
        "aField"
      ],
      "extensions": {
        "remotePath": [
          "viewer",
          "obj"
        ],
        "remoteLocations": [
          {
            "line": 1,
            "column": 38
          }
        ]
      }
    }
  ],
  "data": {
    "viewer": {
      "userId": "456",
      "name": "string",
      "obj": {
        "aField": null,
        "bField": "string"
      }
    }
  }
}
```

## QueryPlan

```json
{
  "document": "{ viewer { userId name obj { aField bField } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "a",
            "document": "query fetch_viewer_1 { viewer { name obj { aField } } }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "b",
            "document": "query fetch_viewer_2 { viewer { obj { bField } } }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "b",
            "document": "query fetch_viewer_3 { viewer { userId } }",
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

