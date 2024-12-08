# Skipped_Root_Selection_Same_Selection_Without_Skip_In_Fragment

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
  productBySlug(slug: $slug) @skip(if: $skip) {
    name
  }
  ... QueryFragment
}

fragment QueryFragment on Query {
  productBySlug(slug: $slug) {
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
      "document": "query($skip: Boolean!, $slug: String!) { productBySlug(slug: $slug) @skip(if: $skip) { name } productBySlug(slug: $slug) { name } }"
    }
  ]
}
```

