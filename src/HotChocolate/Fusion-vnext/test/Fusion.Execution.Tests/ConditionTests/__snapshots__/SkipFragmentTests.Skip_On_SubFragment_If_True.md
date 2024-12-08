# Skip_On_SubFragment_If_True

## Request

```graphql
query GetProduct($slug: String!) {
  productBySlug(slug: $slug) {
    ... ProductFragment @skip(if: true)
    description
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
      "document": "query($slug: String!) { productBySlug(slug: $slug) { description } }"
    }
  ]
}
```

