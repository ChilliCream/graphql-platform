# Skip_On_RootFragment_Only_Skipped_Fragment_Selected

## Request

```graphql
query GetProduct($slug: String!, $skip: Boolean!) {
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
          "document": "query($slug: String!) { ... on Query { productBySlug(slug: $slug) { name } } }"
        }
      ]
    }
  ]
}
```

