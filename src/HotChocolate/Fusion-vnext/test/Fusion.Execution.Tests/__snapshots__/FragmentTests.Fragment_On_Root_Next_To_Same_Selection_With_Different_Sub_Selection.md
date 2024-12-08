# Fragment_On_Root_Next_To_Same_Selection_With_Different_Sub_Selection

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) {
    description
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
      "document": "query($slug: String!) { productBySlug(slug: $slug) { description } productBySlug(slug: $slug) { name } }"
    }
  ]
}
```

