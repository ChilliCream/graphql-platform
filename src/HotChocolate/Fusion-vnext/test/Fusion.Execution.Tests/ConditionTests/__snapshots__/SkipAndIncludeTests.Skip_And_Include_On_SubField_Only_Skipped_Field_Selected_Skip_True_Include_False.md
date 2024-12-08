# Skip_And_Include_On_SubField_Only_Skipped_Field_Selected_Skip_True_Include_False

## Request

```graphql
query GetProduct($slug: String!) {
  productBySlug(slug: $slug) {
    name @skip(if: true) @include(if: false)
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
      "document": "query($slug: String!) { productBySlug(slug: $slug) { __typename } }"
    }
  ]
}
```

