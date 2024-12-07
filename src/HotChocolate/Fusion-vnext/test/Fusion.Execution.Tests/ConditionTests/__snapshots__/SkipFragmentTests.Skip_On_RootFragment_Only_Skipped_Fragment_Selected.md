# Skip_On_RootFragment_Only_Skipped_Fragment_Selected

## Request

```graphql
query GetProduct($id: ID!, $skip: Boolean!) {
  ... QueryFragment @skip(if: $skip)
}

fragment QueryFragment on Query {
  productById(id: $id) {
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
          "document": "{ ... on Query { productById(id: $id) { name } } }"
        }
      ]
    }
  ]
}
```

