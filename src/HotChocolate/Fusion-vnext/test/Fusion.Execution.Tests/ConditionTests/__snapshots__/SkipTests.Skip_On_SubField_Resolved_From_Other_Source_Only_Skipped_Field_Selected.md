# Skip_On_SubField_Resolved_From_Other_Source_Only_Skipped_Field_Selected

## Request

```graphql
query GetProduct($id: ID!, $skip: Boolean!) {
  productById(id: $id) {
    name
    reviews(first: 10) @skip(if: $skip) {
      nodes {
        body
      }
    }
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
      "document": "query($id: ID!) { productById(id: $id) { name } }",
      "nodes": [
        {
          "kind": "Condition",
          "variableName": "skip",
          "passingValue": false,
          "nodes": [
            {
              "kind": "Operation",
              "schema": "REVIEWS",
              "document": "{ productById { reviews(first: 10) { nodes { body } } } }"
            }
          ]
        }
      ]
    }
  ]
}
```

