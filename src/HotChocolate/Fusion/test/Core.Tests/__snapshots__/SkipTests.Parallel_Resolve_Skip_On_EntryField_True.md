# Parallel_Resolve_Skip_On_EntryField

## Result

```json
{
  "data": {
    "viewer": {
      "name": "string"
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
C68ECB4ECD7513E1EB4BBC1EF82D212335091BE6
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
            "document": "query Test_2 { other { userId } }",
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

