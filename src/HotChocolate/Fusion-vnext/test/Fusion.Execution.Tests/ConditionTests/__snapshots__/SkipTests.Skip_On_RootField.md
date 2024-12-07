# Skip_On_RootField

## Request

```graphql
query GetProduct($id: ID!, $skip: Boolean!) {
  productById(id: $id) @skip(if: $skip) {
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
      "document": "query($id: ID!, $skip: Boolean!) { productById(id: $id) @skip(if: $skip) { name } products { nodes { name } } }"
    }
  ]
}
```

