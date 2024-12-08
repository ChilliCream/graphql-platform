# Skip_On_SubFragment_If_False

## Request

```graphql
query GetProduct($slug: String!) {
  productBySlug(slug: $slug) {
    ... ProductFragment @skip(if: false)
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
      "document": "query($slug: String!) { productBySlug(slug: $slug) { name description } }"
    }
  ]
}
```

