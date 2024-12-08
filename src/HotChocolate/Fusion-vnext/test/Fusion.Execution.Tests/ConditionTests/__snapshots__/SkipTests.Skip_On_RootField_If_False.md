# Skip_On_RootField_If_False

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) @skip(if: false) {
    name
  }
  products {
    nodes {
      name
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
      "document": "query($id: ID!) { productById(id: $id) { name } products { nodes { name } } }"
    }
  ]
}
```

