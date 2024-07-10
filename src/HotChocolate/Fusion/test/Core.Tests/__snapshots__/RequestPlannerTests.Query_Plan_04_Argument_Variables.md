# Query_Plan_04_Argument_Variables

## UserRequest

```graphql
query TopProducts($first: Int!) {
  topProducts(first: $first) {
    id
  }
}
```

## QueryPlan

```json
{
  "document": "query TopProducts($first: Int!) { topProducts(first: $first) { id } }",
  "operation": "TopProducts",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Products",
        "document": "query TopProducts_1($first: Int!) { topProducts(first: $first) { id } }",
        "selectionSetId": 0,
        "forwardedVariables": [
          {
            "variable": "first"
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

