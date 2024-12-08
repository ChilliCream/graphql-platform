# Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_Skip_False

## Request

```graphql
query GetProduct($slug: String!, $include: Boolean!) {
  productBySlug(slug: $slug) @include(if: $include) @skip(if: false) {
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
      "variableName": "include",
      "passingValue": true,
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

