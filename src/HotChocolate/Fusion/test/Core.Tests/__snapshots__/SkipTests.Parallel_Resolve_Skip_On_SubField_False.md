# Parallel_Resolve_Skip_On_SubField

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
  other {
    userId @skip(if: $skip)
  }
}
```

## QueryPlan Hash

```text
424991873692932F36F2ED2FCF024547AD0AD47A
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { viewer { name } other { userId @skip(if: $skip) } }",
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
            "document": "query Test_2($skip: Boolean!) { other { userId @skip(if: $skip) } }",
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

