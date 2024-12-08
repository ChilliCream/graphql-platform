# InlineFragment_On_Sub_Selection_Next_To_Different_Selection_From_Different_Subgraph

## Request

```graphql
query GetProduct($slug: String!) {
  productBySlug(slug: $slug) {
    name
    ... {
      averageRating
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
      "document": "query($slug: String!) { productBySlug(slug: $slug) { name } }",
      "nodes": [
        {
          "kind": "Operation",
          "schema": "REVIEWS",
          "document": "{ productById { averageRating } }"
        }
      ]
    }
  ]
}
```

