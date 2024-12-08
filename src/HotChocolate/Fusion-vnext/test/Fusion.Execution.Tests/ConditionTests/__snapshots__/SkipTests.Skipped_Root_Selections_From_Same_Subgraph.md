# Skipped_Root_Selections_From_Same_Subgraph

## Request

```graphql
query($slug: String!, $skip1: Boolean!, $skip2: Boolean!) {
  productBySlug(slug: $slug) @skip(if: $skip1) {
    name
  }
  products @skip(if: $skip2) {
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
      "document": "query($skip1: Boolean!, $skip2: Boolean!, $slug: String!) { productBySlug(slug: $slug) @skip(if: $skip1) { name } products @skip(if: $skip2) { nodes { name } } }"
    }
  ]
}
```

