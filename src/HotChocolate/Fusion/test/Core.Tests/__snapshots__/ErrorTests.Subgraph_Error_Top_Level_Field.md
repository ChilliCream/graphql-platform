# Subgraph_Error_Top_Level_Field

## User Request

```graphql
{
  viewer {
    data {
      accountValue
    }
  }
  errorField
}
```

## Result

```json
{
  "errors": [
    {
      "message": "SOME TOP LEVEL USER ERROR",
      "path": [
        "errorField"
      ],
      "extensions": {
        "remotePath": [
          "errorField"
        ],
        "remoteLocations": [
          {
            "line": 1,
            "column": 68
          }
        ]
      }
    }
  ],
  "data": {
    "viewer": {
      "data": {
        "accountValue": "Account"
      }
    },
    "errorField": null
  }
}
```

## QueryPlan

```json
{
  "document": "{ viewer { data { accountValue } } errorField }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Accounts",
        "document": "query fetch_viewer_errorField_1 { viewer { data { accountValue } } errorField }",
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

