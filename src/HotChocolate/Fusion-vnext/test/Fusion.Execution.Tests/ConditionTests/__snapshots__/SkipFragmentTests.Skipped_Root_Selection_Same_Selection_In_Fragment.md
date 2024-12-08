# Skipped_Root_Selection_Same_Selection_In_Fragment

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
  productBySlug(slug: $slug) @skip(if: $skip) {
    name
  }
  ... QueryFragment
}

fragment QueryFragment on Query {
  productBySlug(slug: $slug) @skip(if: $skip) {
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
      "kind": "Condition",
      "variableName": "skip",
      "passingValue": false,
      "nodes": [
        {
          "kind": "Operation",
          "schema": "PRODUCTS",
          "document": "query($slug: String!) { productBySlug(slug: $slug) { name } }"
        }
      ]
    }
  ]
}
```

