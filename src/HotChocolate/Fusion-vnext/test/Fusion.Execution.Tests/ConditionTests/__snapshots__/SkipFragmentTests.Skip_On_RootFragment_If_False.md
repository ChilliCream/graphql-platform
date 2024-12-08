# Skip_On_RootFragment_If_False

## Request

```graphql
query GetProduct($id: ID!) {
  ... QueryFragment @skip(if: false)
  products {
    nodes {
      name
    }
  }
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
      "schema": "PRODUCTS",
      "document": "query($id: ID!) { productById(id: $id) { name } products { nodes { name } } }"
    }
  ]
}
```

