# TopLevelResolveSubgraphError

## Result

```json
{
  "errors": [
    {
      "message": "SOME TOP LEVEL USER ERROR",
      "locations": [
        {
          "line": 1,
          "column": 68
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
    "viewer": {
      "data": {
        "accountValue": "Account"
      }
    },
    "errorField": null
  }
}
```

## Request

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

## QueryPlan Hash

```text
9578FF2608B68C6D9AE96CD13B57F603C4554FFF
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

