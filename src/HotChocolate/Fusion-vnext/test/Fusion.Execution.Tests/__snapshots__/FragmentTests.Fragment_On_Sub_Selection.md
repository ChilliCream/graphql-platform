# Fragment_On_Sub_Selection

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    ... ProductFragment
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
      "document": "query($id: ID!) { productById(id: $id) { name } }"
    }
  ]
}
```

