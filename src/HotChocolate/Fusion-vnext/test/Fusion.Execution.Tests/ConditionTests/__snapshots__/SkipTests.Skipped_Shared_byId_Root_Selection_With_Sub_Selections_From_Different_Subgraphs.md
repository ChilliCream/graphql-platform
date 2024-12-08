# Skipped_Shared_byId_Root_Selection_With_Sub_Selections_From_Different_Subgraphs

## Request

```graphql
query($id: ID!) {
  productById(id: $id) @skip(if: $skip) {
    name
    averageRating
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
          "schema": "REVIEWS",
          "document": "query($id: ID!) { productById(id: $id) { averageRating } }"
        }
      ]
    }
  ]
}
```

