# Skipped_Sub_Selection_If_True

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) {
    name @skip(if: true)
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
      "document": "query($slug: String!) { productBySlug(slug: $slug) { __typename } }"
    }
  ]
}
```

