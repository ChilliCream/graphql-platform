# Skipped_Sub_Selection_If_False

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) {
    name @skip(if: false)
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

