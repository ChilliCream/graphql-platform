# Two_InlineFragments_On_Root_With_Different_Selection_From_Different_Subgraph

## Request

```graphql
query GetProduct($id: ID!) {
  ... {
    productById(id: $id) {
      name
    }
  }
  ... {
    viewer {
      displayName
    }
  }
}
```

## Plan

```json
{
  "kind": "Root",
  "nodes": [
    {
      "kind": "Operation",
      "schema": "PRODUCTS",
      "document": "query($id: ID!) { productById(id: $id) { name } }"
    },
    {
      "kind": "Operation",
      "schema": "ACCOUNTS",
      "document": "{ viewer { displayName } }"
    }
  ]
}
```

