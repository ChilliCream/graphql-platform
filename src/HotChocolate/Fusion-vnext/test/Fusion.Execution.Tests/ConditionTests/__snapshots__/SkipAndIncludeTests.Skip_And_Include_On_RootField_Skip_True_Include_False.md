# Skip_And_Include_On_RootField_Skip_True_Include_False

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) @skip(if: true) @include(if: false) {
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

