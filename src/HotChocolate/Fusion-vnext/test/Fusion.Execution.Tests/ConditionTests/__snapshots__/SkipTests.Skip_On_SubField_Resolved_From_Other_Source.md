# Skip_On_SubField_Resolved_From_Other_Source

## Request

```graphql
query GetProduct($id: ID!, $skip: Boolean!) {
  productById(id: $id) {
    name
    averageRating
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
          "kind": "Operation",
          "schema": "REVIEWS",
          "document": "query($skip: Boolean!) { productById { averageRating reviews(first: 10) @skip(if: $skip) { nodes { body } } } }"
        }
      ]
    }
  ]
}
```

