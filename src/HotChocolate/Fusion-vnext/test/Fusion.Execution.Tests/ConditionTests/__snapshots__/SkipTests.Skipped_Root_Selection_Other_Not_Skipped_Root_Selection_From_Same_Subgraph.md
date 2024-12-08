# Skipped_Root_Selection_Other_Not_Skipped_Root_Selection_From_Same_Subgraph

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
  productBySlug(slug: $slug) @skip(if: $skip) {
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
      "document": "query($skip: Boolean!, $slug: String!) { productBySlug(slug: $slug) @skip(if: $skip) { name } products { nodes { name } } }"
    }
  ]
}
```

