# Skip_On_SubField_From_Different_Subgraph_If_True

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    name
    averageRating
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
      "document": "query($id: ID!) { productById(id: $id) { name } }",
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
```

