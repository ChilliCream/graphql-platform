# Skip_On_RootFragment_If_True

## Request

```graphql
query GetProduct($slug: String!) {
  ... QueryFragment @skip(if: true)
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
      "document": "{ products { nodes { name } } }"
    }
  ]
}
```

