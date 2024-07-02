# ReplaceMessageWithErrorFilter

## Result

```json
{
  "errors": [
    {
      "message": "REPLACED MESSAGE",
      "locations": [
        {
          "line": 1,
          "column": 28
        }
      ],
      "path": [
        "errorField"
      ],
      "extensions": {
        "remotePath": [
          "errorField"
        ]
      }
    }
  ],
  "data": {
    "errorField": null
  }
}
```

## Request

```graphql
{
  errorField
}
```

## QueryPlan Hash

```text
EFD2B7A711B25BA08C9508F52A4E39DDD1E4534E
```

## QueryPlan

```json
{
  "document": "{ errorField }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Accounts",
        "document": "query fetch_errorField_1 { errorField }",
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

