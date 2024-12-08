# Fragment_On_Sub_Selection

## Request

```graphql
query GetProduct($slug: String!) {
  productBySlug(slug: $slug) {
    ... ProductFragment
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

