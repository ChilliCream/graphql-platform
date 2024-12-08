# InlineFragment_On_Sub_Selection_Next_To_Different_Selection

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) {
    name
    ... {
      description
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
      "document": "query($slug: String!) { productBySlug(slug: $slug) { name description } }"
    }
  ]
}
```

