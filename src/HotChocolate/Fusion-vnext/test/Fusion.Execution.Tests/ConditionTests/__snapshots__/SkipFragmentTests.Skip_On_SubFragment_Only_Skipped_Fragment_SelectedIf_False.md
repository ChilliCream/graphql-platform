# Skip_On_SubFragment_Only_Skipped_Fragment_SelectedIf_False

## Request

```graphql
query GetProduct($slug: String!) {
  productBySlug(slug: $slug) {
    ... ProductFragment @skip(if: false)
  }
}

fragment ProductFragment on Product {
  name
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

