# Skip_And_Include_On_RootField_Skip_True_Include_True

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) @skip(if: true) @include(if: true) {
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

