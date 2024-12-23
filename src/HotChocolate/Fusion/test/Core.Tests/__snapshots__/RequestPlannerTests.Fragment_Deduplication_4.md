# Fragment_Deduplication_4

## UserRequest

```graphql
{
  field1
  ... on Query {
    field2
  }
  ... query
}

fragment query on Query {
  field1
  field2
}
```

## QueryPlan

```json
{
  "document": "{ field1 ... on Query { field2 } ... query } fragment query on Query { field1 field2 }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_field1_field2_1 { field1 field2 }",
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

