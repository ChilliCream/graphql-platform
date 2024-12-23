# Fragment_Deduplication_6

## UserRequest

```graphql
query test($skip: Boolean!) {
  field1
  ... on Query @skip(if: $skip) {
    field2
  }
  ... query
}

fragment query on Query {
  field1
}
```

## QueryPlan

```json
{
  "document": "query test($skip: Boolean!) { field1 ... on Query @skip(if: $skip) { field2 } ... query } fragment query on Query { field1 }",
  "operation": "test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query test_1 { field1 field2 }",
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

