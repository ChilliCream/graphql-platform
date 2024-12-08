# Skipped_Sub_Selection

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
  productBySlug(slug: $slug) {
    name @skip(if: $skip)
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
      "document": "query($skip: Boolean!, $slug: String!) { productBySlug(slug: $slug) { name @skip(if: $skip) } }"
    }
  ]
}
```

