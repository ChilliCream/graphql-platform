# Skipped_Sub_Selection_Other_Not_Skipped_Sub_Selection_From_Same_Subgraph_If_True

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) {
    name @skip(if: true)
    description
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
      "document": "query($slug: String!) { productBySlug(slug: $slug) { description } }"
    }
  ]
}
```

