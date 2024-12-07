# InlineFragment_On_Root_Next_To_Same_Selection

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    name
  }
  ... {
    productById(id: $id) {
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
      "kind": "Operation",
      "schema": "PRODUCTS",
      "document": "query($id: ID!) { productById(id: $id) { name } }"
    }
  ]
}
```

