# Multiple_Skip_On_SubFields_From_Different_Subgraph

## Request

```graphql
query GetProduct($id: ID!, $skip1: Boolean!, $skip2: Boolean!) {
  productById(id: $id) {
    id
    name @skip(if: $skip1)
    averageRating @skip(if: $skip2)
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
      "document": "query($id: ID!, $skip1: Boolean!) { productById(id: $id) { id name @skip(if: $skip1) } }",
      "nodes": [
        {
          "kind": "Condition",
          "variableName": "skip2",
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

