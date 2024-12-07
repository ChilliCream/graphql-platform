# Fragment_On_Root_Next_To_Different_Selection_From_Different_Subgraph

## Request

```graphql
query GetProduct($id: ID!) {
  viewer {
    displayName
  }
  ... QueryFragment
}

fragment QueryFragment on Query {
  productById(id: $id) {
    name
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
      "schema": "ACCOUNTS",
      "document": "{ viewer { displayName } }"
    },
    {
      "kind": "Operation",
      "schema": "PRODUCTS",
      "document": "query($id: ID!) { productById(id: $id) { name } }"
    }
  ]
}
```

