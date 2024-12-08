# Two_InlineFragments_On_Root_With_Same_Selection

## Request

```graphql
query GetProduct($slug: String!) {
  ... {
    productBySlug(slug: $slug) {
      name
    }
  }
  ... {
    productBySlug(slug: $slug) {
      name
    }
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

