# Two_Fragments_On_Sub_Selection_With_Different_Selection_From_Different_Subgraph

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    ... ProductFragment1
    ... ProductFragment2
  }
}

fragment ProductFragment1 on Product {
  name
}

fragment ProductFragment2 on Product {
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

