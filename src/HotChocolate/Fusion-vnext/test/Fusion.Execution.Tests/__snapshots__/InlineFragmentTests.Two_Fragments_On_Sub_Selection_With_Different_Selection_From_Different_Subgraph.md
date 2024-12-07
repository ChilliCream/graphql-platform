# Two_Fragments_On_Sub_Selection_With_Different_Selection_From_Different_Subgraph

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    ... {
      name
    }
    ... {
      averageRating
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

