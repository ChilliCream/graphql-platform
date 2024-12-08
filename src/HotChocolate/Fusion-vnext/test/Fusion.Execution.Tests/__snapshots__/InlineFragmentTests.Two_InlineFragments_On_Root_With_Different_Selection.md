# Two_InlineFragments_On_Root_With_Different_Selection

## Request

```graphql
query($slug: String!) {
  ... {
    productBySlug(slug: $slug) {
      name
    }
  }
  ... {
    products {
      nodes {
        description
      }
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
      "document": "query($slug: String!) { productBySlug(slug: $slug) { name } products { nodes { description } } }"
    }
  ]
}
```

