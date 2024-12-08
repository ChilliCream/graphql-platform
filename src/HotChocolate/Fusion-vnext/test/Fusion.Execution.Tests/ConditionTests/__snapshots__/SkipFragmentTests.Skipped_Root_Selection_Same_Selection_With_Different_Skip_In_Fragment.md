# Skipped_Root_Selection_Same_Selection_With_Different_Skip_In_Fragment

## Request

```graphql
query($slug: String!, $skip1: Boolean!, $skip2: Boolean!) {
  productBySlug(slug: $slug) @skip(if: $skip1) {
    name
  }
  ... QueryFragment
}

fragment QueryFragment on Query {
  productBySlug(slug: $slug) @skip(if: $skip2) {
    name
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
      "document": "query($skip1: Boolean!, $skip2: Boolean!, $slug: String!) { productBySlug(slug: $slug) @skip(if: $skip1) { name } productBySlug(slug: $slug) @skip(if: $skip2) { name } }"
    }
  ]
}
```

