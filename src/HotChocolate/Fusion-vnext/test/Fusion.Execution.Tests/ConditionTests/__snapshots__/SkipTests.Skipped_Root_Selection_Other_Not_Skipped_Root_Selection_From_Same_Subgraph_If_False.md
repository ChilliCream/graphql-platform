# Skipped_Root_Selection_Other_Not_Skipped_Root_Selection_From_Same_Subgraph_If_False

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) @skip(if: false) {
    name
  }
  products {
    nodes {
      name
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
      "document": "query($slug: String!) { productBySlug(slug: $slug) { name } products { nodes { name } } }"
    }
  ]
}
```

