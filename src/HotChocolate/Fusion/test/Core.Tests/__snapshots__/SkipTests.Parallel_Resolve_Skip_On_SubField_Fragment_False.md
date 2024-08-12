# Parallel_Resolve_Skip_On_SubField_Fragment

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
    ... Test @skip(if: $skip)
  }
}

fragment Test on Other {
  userId
}
```

## QueryPlan Hash

```text
673C1AC76B07700826D5C27DD146138C765DA6CB
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { viewer { name } other { ... Test @skip(if: $skip) } } fragment Test on Other { userId }",
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

