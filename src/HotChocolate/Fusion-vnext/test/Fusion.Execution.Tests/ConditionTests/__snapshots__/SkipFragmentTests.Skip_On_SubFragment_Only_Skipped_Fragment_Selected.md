# Skip_On_SubFragment_Only_Skipped_Fragment_Selected

## Request

```graphql
query GetProduct($slug: String!, $skip: Boolean!) {
  productBySlug(slug: $slug) {
    ... ProductFragment @skip(if: $skip)
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
      "document": "query($skip: Boolean!, $slug: String!) { productBySlug(slug: $slug) { ... on Product @skip(if: $skip) { name } } }"
    }
  ]
}
```

