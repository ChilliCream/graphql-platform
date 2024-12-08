# Skip_On_RootFragment_If_False

## Request

```graphql
query GetProduct($slug: String!) {
  ... QueryFragment @skip(if: false)
  products {
    nodes {
      name
    }
  }
}

fragment QueryFragment on Query {
  productBySlug(slug: $slug) {
    name
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
      "document": "query($slug: String!) { productBySlug(slug: $slug) { name } products { nodes { name } } }"
    }
  ]
}
```

