# Skip_On_RootField_Only_Skipped_Field_Selected

## Request

```graphql
query GetProduct($id: ID!, $skip: Boolean!) {
  productById(id: $id) @skip(if: $skip) {
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
          "document": "query($id: ID!) { productById(id: $id) { name } }"
        }
      ]
    }
  ]
}
```

