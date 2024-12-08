# Skip_And_Include_On_RootField

## Request

```graphql
query GetProduct($id: ID!, $skip: Boolean!, $include: Boolean!) {
  productById(id: $id) @skip(if: $skip) @include(if: $include) {
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
      "document": "query($id: ID!, $include: Boolean!, $skip: Boolean!) { productById(id: $id) @skip(if: $skip) @include(if: $include) { name } products { nodes { name } } }"
    }
  ]
}
```

