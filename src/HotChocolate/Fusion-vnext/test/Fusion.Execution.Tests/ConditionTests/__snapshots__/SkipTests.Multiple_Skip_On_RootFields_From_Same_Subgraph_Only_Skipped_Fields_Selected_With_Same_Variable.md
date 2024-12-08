# Multiple_Skip_On_RootFields_From_Same_Subgraph_Only_Skipped_Fields_Selected_With_Same_Variable

## Request

```graphql
query GetProduct($id: ID!, $skip: Boolean!) {
  productById(id: $id) @skip(if: $skip) {
    name
  }
  products @skip(if: $skip) {
    nodes {
      name
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
      "kind": "Condition",
      "variableName": "skip",
      "passingValue": false,
      "nodes": [
        {
          "kind": "Operation",
          "schema": "PRODUCTS",
          "document": "query($id: ID!) { productById(id: $id) { name } products { nodes { name } } }"
        }
      ]
    }
  ]
}
```

