# Parallel_Resolve_Skip_On_EntryField

## Result

```json
{
  "data": {
    "viewer": {
      "name": "string"
    },
    "other": {
      "userId": "1"
    }
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  viewer {
    name
  }
  other @skip(if: $skip) {
    userId
  }
}
```

## QueryPlan Hash

```text
EBF12464BEA547C7E5B76B77619C730E63F9BB78
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { viewer { name } other @skip(if: $skip) { userId } }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Subgraph_1",
            "document": "query Test_1 { viewer { name } }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query Test_2($skip: Boolean!) { other @skip(if: $skip) { userId } }",
            "selectionSetId": 0,
            "forwardedVariables": [
              {
                "variable": "skip"
              }
            ]
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

