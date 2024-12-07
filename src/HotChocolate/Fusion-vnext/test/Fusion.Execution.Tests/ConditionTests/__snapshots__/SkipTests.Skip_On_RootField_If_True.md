# Skip_On_RootField_If_True

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) @skip(if: true) {
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
      "document": "{ products { nodes { name } } }"
    }
  ]
}
```

