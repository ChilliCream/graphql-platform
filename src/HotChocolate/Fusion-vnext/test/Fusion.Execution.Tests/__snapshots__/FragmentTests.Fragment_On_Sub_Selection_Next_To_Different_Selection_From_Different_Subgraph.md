# Fragment_On_Sub_Selection_Next_To_Different_Selection_From_Different_Subgraph

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    name
    ... ProductFragment
  }
}

fragment ProductFragment on Product {
  averageRating
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

