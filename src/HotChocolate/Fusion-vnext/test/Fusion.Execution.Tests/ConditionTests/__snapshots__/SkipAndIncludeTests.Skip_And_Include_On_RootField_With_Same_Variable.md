# Skip_And_Include_On_RootField_With_Same_Variable

## Request

```graphql
query GetProduct($slug: String!, $skipOrInclude: Boolean!) {
  productBySlug(slug: $slug) @skip(if: $skipOrInclude) @include(if: $skipOrInclude) {
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
      "document": "query($skipOrInclude: Boolean!, $slug: String!) { productBySlug(slug: $slug) @skip(if: $skipOrInclude) @include(if: $skipOrInclude) { name } products { nodes { name } } }"
    }
  ]
}
```

