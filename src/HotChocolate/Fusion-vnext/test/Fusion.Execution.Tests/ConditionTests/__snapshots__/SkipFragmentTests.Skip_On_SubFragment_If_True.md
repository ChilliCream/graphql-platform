# Skip_On_SubFragment_If_True

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    ... ProductFragment @skip(if: true)
    description
  }
}

fragment ProductFragment on Product {
  name
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
      "document": "query($id: ID!) { productById(id: $id) { description } }"
    }
  ]
}
```

