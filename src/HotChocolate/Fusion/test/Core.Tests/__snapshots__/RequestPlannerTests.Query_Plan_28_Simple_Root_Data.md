# Query_Plan_28_Simple_Root_Data

## UserRequest

```graphql
query Query {
  data {
    a
    b
  }
}
```

## QueryPlan

```json
{
  "document": "query Query { data { a b } }",
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
            "document": "query Query_1 { data { a } }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "B",
            "document": "query Query_2 { data { b } }",
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

