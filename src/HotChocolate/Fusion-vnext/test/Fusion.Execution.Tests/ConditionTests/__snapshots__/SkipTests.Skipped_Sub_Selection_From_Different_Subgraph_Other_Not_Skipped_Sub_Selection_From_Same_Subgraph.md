# Skipped_Sub_Selection_From_Different_Subgraph_Other_Not_Skipped_Sub_Selection_From_Same_Subgraph

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
  productBySlug(slug: $slug) {
    averageRating @skip(if: $skip)
    reviews(first: 10) {
      nodes {
        body
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
      "document": "query($slug: String!) { productBySlug(slug: $slug) }",
      "nodes": [
        {
          "kind": "Operation",
          "schema": "REVIEWS",
          "document": "query($skip: Boolean!) { productById { averageRating @skip(if: $skip) reviews(first: 10) { nodes { body } } } }"
        }
      ]
    }
  ]
}
```

