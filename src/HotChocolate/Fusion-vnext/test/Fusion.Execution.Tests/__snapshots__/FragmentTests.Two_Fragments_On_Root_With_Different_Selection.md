# Two_Fragments_On_Root_With_Different_Selection

## Request

```graphql
query GetProduct($slug: String!) {
  ... QueryFragment1
  ... QueryFragment2
}

fragment QueryFragment1 on Query {
  productBySlug(slug: $slug) {
    name
  }
}

fragment QueryFragment2 on Query {
  products {
    nodes {
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
      "document": "query($slug: String!) { productBySlug(slug: $slug) { name } products { nodes { description } } }"
    }
  ]
}
```

