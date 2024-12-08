# Skipped_Sub_Selection_From_Different_Subgraph_Other_Not_Skipped_Sub_Selection_From_First_Subgraph

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
  productBySlug(slug: $slug) {
    name
    averageRating @skip(if: $skip)
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
          "kind": "Condition",
          "variableName": "skip",
          "passingValue": false,
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
  ]
}
```

