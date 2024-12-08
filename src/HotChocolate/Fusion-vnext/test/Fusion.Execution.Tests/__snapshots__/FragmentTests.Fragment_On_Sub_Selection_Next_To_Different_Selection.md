# Fragment_On_Sub_Selection_Next_To_Different_Selection

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    name
    ... ProductFragment
  }
}

fragment ProductFragment on Product {
  description
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
      "document": "query($id: ID!) { productById(id: $id) { name description } }"
    }
  ]
}
```

