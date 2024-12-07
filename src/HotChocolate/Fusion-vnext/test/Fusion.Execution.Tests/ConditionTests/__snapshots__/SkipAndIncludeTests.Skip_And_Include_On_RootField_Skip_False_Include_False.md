# Skip_And_Include_On_RootField_Skip_False_Include_False

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) @skip(if: false) @include(if: false) {
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

