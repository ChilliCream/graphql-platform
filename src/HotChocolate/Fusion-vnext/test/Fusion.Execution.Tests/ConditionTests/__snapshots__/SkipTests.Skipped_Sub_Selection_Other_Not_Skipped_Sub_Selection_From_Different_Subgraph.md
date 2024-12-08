# Skipped_Sub_Selection_Other_Not_Skipped_Sub_Selection_From_Different_Subgraph

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
  productBySlug(slug: $slug) {
    name @skip(if: $skip)
    averageRating
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
      "document": "query($skip: Boolean!, $slug: String!) { productBySlug(slug: $slug) { name @skip(if: $skip) } }",
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

