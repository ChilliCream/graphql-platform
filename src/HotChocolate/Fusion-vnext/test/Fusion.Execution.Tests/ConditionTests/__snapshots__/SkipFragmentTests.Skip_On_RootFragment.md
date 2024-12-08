# Skip_On_RootFragment

## Request

```graphql
query GetProduct($slug: String!, $skip: Boolean!) {
  ... QueryFragment @skip(if: $skip)
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
      "document": "query($skip: Boolean!, $slug: String!) { ... on Query @skip(if: $skip) { productBySlug(slug: $slug) { name } } products { nodes { name } } }"
    }
  ]
}
```

