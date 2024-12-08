# Multiple_Skip_On_SubFields_From_Different_Subgraph_Only_Skipped_Fields_Selected_With_Same_Variable

## Request

```graphql
query GetProduct($id: ID!, $skip: Boolean!) {
  productById(id: $id) {
    name @skip(if: $skip)
    averageRating @skip(if: $skip)
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
      "document": "query($id: ID!, $skip: Boolean!) { productById(id: $id) { name @skip(if: $skip) } }",
      "nodes": [
        {
          "kind": "Condition",
          "variableName": "skip",
          "passingValue": false,
          "nodes": [
            {
              "kind": "Operation",
              "schema": "REVIEWS",
              "document": "{ productById { averageRating } }"
            }
          ]
        }
      ]
    }
  ]
}
```

