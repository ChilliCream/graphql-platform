# Skip_And_Include_On_RootField_Only_Skipped_Field_Selected

## Request

```graphql
query GetProduct($slug: String!, $skipOrInclude: Boolean!) {
  productBySlug(slug: $slug) @skip(if: $skipOrInclude) @include(if: $skipOrInclude) {
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
      "variableName": "skipOrInclude",
      "passingValue": false,
      "nodes": [
        {
          "kind": "Condition",
          "variableName": "skipOrInclude",
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
  ]
}
```

