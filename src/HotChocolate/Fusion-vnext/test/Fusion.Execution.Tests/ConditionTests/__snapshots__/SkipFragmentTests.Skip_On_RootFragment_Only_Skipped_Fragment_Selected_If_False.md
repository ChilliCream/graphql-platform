# Skip_On_RootFragment_Only_Skipped_Fragment_Selected_If_False

## Request

```graphql
query GetProduct($slug: String!) {
  ... QueryFragment @skip(if: false)
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
      "document": "query($slug: String!) { productBySlug(slug: $slug) { name } }"
    }
  ]
}
```

