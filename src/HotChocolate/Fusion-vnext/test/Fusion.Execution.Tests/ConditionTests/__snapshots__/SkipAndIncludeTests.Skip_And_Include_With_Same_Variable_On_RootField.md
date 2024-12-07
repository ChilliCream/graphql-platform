# Skip_And_Include_With_Same_Variable_On_RootField

## Request

```graphql
query GetProduct($id: ID!, $skipOrInclude: Boolean!) {
  productById(id: $id) @skip(if: $skipOrInclude) @include(if: $skipOrInclude) {
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
      "document": "query($id: ID!, $skipOrInclude: Boolean!) { productById(id: $id) @skip(if: $skipOrInclude) @include(if: $skipOrInclude) { name } products { nodes { name } } }"
    }
  ]
}
```

