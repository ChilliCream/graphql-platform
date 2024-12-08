# Fragment_On_Root_Next_To_Same_Selection

## Request

```graphql
query GetProduct($slug: String!) {
  productBySlug(slug: $slug) {
    name
  }
  ... QueryFragment
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

