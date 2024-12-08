# Two_Fragments_On_Root_With_Different_Selection_From_Different_Subgraph

## Request

```graphql
query GetProduct($id: ID!) {
  ... QueryFragment1
  ... QueryFragment2
}

fragment QueryFragment1 on Query {
  productById(id: $id) {
    name
  }
}

fragment QueryFragment2 on Query {
  viewer {
    displayName
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

