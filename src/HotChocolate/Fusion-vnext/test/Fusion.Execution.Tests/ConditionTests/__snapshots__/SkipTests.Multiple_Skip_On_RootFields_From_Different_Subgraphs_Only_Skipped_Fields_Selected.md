# Multiple_Skip_On_RootFields_From_Different_Subgraphs_Only_Skipped_Fields_Selected

## Request

```graphql
query GetProduct($id: ID!, $skip1: Boolean!, $skip2: Boolean!) {
  productById(id: $id) @skip(if: $skip1) {
    name
  }
  viewer @skip(if: $skip2) {
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
      "variableName": "skip1",
      "passingValue": false,
      "nodes": [
        {
          "kind": "Operation",
          "schema": "PRODUCTS",
          "document": "query($id: ID!) { productById(id: $id) { name } }"
        }
      ]
    },
    {
      "kind": "Condition",
      "variableName": "skip2",
      "passingValue": false,
      "nodes": [
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

