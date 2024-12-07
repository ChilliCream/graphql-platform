# Skip_On_SubField_Resolved_From_Other_Source_If_False

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    name
    averageRating
    reviews(first: 10) @skip(if: false) {
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
          "kind": "Operation",
          "schema": "REVIEWS",
          "document": "{ productById { averageRating reviews(first: 10) { nodes { body } } } }"
        }
      ]
    }
  ]
}
```

