# Skipped_Root_Fragments_With_Same_Root_Selection_From_Same_Subgraph

## Request

```graphql
query($slug: String!, $skip1: Boolean!, $skip2: Boolean!) {
  ... QueryFragment1 @skip(if: $skip1)
  ... QueryFragment2 @skip(if: $skip2)
}

fragment QueryFragment1 on Query {
  productBySlug(slug: $slug) {
    name
  }
}

fragment QueryFragment2 on Query {
  productBySlug(slug: $slug) {
    name
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
      "document": "query($skip1: Boolean!, $skip2: Boolean!, $slug: String!) { ... on Query @skip(if: $skip1) { productBySlug(slug: $slug) { name } } ... on Query @skip(if: $skip2) { productBySlug(slug: $slug) { name description } } }"
    }
  ]
}
```

