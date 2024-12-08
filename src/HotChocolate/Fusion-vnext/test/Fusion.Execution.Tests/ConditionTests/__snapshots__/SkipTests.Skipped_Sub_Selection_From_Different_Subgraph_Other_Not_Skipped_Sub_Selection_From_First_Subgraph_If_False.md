# Skipped_Sub_Selection_From_Different_Subgraph_Other_Not_Skipped_Sub_Selection_From_First_Subgraph_If_False

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) {
    name
    averageRating @skip(if: false)
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

