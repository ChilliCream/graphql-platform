# Skipped_Root_Selection_If_False

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) @skip(if: false) {
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

