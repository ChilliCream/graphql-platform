# Query_Plan_30_Entity_Data

## UserRequest

```graphql
query Query {
  entity(id: 123) {
    a
    b
  }
}
```

## QueryPlan

```json
{
  "document": "query Query { entity(id: 123) { a b } }",
  "operation": "Query",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "A",
            "document": "query Query_1 { entity(id: 123) { a } }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "B",
            "document": "query Query_2 { entity(id: 123) { b } }",
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

