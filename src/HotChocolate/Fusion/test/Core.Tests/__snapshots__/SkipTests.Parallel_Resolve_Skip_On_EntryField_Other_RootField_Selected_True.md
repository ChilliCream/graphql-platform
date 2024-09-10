# Parallel_Resolve_Skip_On_EntryField_Other_RootField_Selected

## Result

```json
{
  "data": {
    "viewer": {
      "name": "string"
    },
    "another": "string"
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
  another
}
```

## QueryPlan Hash

```text
A1607582FDA8ACE7201DD6EECD6EF8922C8D9043
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { viewer { name } other @skip(if: $skip) { userId } another }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query Test_1($skip: Boolean!) { other @skip(if: $skip) { userId } another }",
            "selectionSetId": 0,
            "forwardedVariables": [
              {
                "variable": "skip"
              }
            ]
          },
          {
            "type": "Resolve",
            "subgraph": "Subgraph_1",
            "document": "query Test_2 { viewer { name } }",
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

