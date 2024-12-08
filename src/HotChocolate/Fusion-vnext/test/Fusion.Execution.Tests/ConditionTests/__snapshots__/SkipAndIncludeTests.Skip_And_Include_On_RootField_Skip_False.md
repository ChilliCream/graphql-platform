# Skip_And_Include_On_RootField_Skip_False

## Request

```graphql
query GetProduct($id: ID!, $include: Boolean!) {
  productById(id: $id) @include(if: $include) @skip(if: false) {
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
      "document": "query($id: ID!, $include: Boolean!) { productById(id: $id) @include(if: $include) { name } products { nodes { name } } }"
    }
  ]
}
```

