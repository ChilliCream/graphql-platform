# InlineFragment_On_Root_Next_To_Different_Selection

## Request

```graphql
query GetProduct($slug: String!) {
  products {
    nodes {
      description
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
      "document": "query($slug: String!) { products { nodes { description } } productBySlug(slug: $slug) { name } }"
    }
  ]
}
```

