# Skip_And_Include_With_Same_Variable_On_RootField_Only_Skipped_Field_Selected

## Request

```graphql
query GetProduct($id: ID!, $skip: Boolean!, $include: Boolean!) {
  productById(id: $id) @skip(if: $skip) @include(if: $include) {
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
          "kind": "Condition",
          "variableName": "include",
          "passingValue": true,
          "nodes": [
            {
              "kind": "Operation",
              "schema": "PRODUCTS",
              "document": "{ productById(id: $id) { name } }"
            }
          ]
        }
      ]
    }
  ]
}
```

