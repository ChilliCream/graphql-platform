# Parallel_Resolve_Skip_On_EntryField_Other_RootField_Selected

## Result

```json
{
  "data": {
    "viewer": {
      "name": "string"
    },
    "other": {
      "userId": "1"
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
475C1241AAB636FB74A719B919BDD84C2178326A
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
            "document": "query Test_1 { other { userId } another }",
            "selectionSetId": 0
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

