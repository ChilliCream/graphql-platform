# Skip_And_Include_On_RootField_Skip_True

## Request

```graphql
query GetProduct($id: ID!, $include: Boolean!) {
  productById(id: $id) @include(if: $include) @skip(if: true) {
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

