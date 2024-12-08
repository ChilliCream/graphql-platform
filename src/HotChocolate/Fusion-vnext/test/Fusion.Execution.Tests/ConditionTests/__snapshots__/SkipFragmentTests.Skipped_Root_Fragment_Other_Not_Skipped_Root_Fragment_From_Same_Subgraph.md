# Skipped_Root_Fragment_Other_Not_Skipped_Root_Fragment_From_Same_Subgraph

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
  ... QueryFragment1 @skip(if: $skip)
  ... QueryFragment2
}

fragment QueryFragment1 on Query {
  productBySlug(slug: $slug) {
    name
  }
}

fragment QueryFragment2 on Query {
  products {
    nodes {
      description
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
      "document": "query($skip: Boolean!, $slug: String!) { ... on Query @skip(if: $skip) { productBySlug(slug: $slug) { name } } products { nodes { description } } }"
    }
  ]
}
```

