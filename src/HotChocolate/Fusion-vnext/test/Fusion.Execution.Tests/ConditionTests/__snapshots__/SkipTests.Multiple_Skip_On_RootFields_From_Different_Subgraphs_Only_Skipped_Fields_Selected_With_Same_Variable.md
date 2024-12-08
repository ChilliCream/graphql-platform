# Multiple_Skip_On_RootFields_From_Different_Subgraphs_Only_Skipped_Fields_Selected_With_Same_Variable

## Request

```graphql
query GetProduct($id: ID!, $skip: Boolean!) {
  productById(id: $id) @skip(if: $skip) {
    name
  }
  viewer @skip(if: $skip) {
    displayName
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
        },
        {
          "kind": "Operation",
          "schema": "ACCOUNTS",
          "document": "{ viewer { displayName } }"
        }
      ]
    }
  ]
}
```

