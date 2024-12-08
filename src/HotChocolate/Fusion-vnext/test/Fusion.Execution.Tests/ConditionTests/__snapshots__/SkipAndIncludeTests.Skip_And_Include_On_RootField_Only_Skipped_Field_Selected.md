# Skip_And_Include_On_RootField_Only_Skipped_Field_Selected

## Request

```graphql
query GetProduct($id: ID!, $skipOrInclude: Boolean!) {
  productById(id: $id) @skip(if: $skipOrInclude) @include(if: $skipOrInclude) {
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
              "document": "query($id: ID!) { productById(id: $id) { name } }"
            }
          ]
        }
      ]
    }
  ]
}
```

