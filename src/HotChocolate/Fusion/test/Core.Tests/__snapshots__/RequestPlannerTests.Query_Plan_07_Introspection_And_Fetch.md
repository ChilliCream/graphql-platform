# Query_Plan_07_Introspection_And_Fetch

## UserRequest

```graphql
query TopProducts($first: Int!) {
  topProducts(first: $first) {
    id
  }
  __schema {
    types {
      name
      kind
      fields {
        name
        type {
          name
          kind
        }
      }
    }
  }
}
```

## QueryPlan

```json
{
  "document": "query TopProducts($first: Int!) { topProducts(first: $first) { id } __schema { types { name kind fields { name type { name kind } } } } }",
  "operation": "TopProducts",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Introspect",
            "document": "{ __schema { types { name kind fields { name type { name kind } } } } }"
          },
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

