# Skipped_Root_Fragment

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
  ... QueryFragment @skip(if: $skip)
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

