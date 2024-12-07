# Skip_On_SubField_Resolved_From_Other_Source_Only_Skipped_Field_Selected_If_True

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    name
    reviews(first: 10) @skip(if: true) {
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
      "document": "query($id: ID!) { productById(id: $id) { name } }"
    }
  ]
}
```

