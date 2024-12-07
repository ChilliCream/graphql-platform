# Fragment_On_Sub_Selection_Next_To_Same_Selection

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    name
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

