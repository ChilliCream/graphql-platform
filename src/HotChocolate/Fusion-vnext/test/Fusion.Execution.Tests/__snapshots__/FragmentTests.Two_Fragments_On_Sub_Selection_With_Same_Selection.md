# Two_Fragments_On_Sub_Selection_With_Same_Selection

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) {
    ... ProductFragment1
    ... ProductFragment2
  }
}

fragment ProductFragment1 on Product {
  name
}

fragment ProductFragment2 on Product {
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

