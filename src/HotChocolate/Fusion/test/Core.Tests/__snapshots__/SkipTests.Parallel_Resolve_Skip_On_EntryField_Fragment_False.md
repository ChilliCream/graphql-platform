# Parallel_Resolve_Skip_On_EntryField_Fragment

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
  ... Test @skip(if: $skip)
}

fragment Test on Query {
  other {
    userId
  }
}
```

## QueryPlan Hash

```text
9C2E1B5B1976B055D0F3E9380B846B70B9831BC3
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { viewer { name } ... Test @skip(if: $skip) } fragment Test on Query { other { userId } }",
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

