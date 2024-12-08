# Fragment_On_Sub_Selection_Next_To_Same_Selection

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) {
    name
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

