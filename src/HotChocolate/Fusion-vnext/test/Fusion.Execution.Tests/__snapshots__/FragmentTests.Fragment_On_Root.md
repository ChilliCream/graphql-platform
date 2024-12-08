# Fragment_On_Root

## Request

```graphql
query GetProduct($id: ID!) {
  ... QueryFragment
}

fragment QueryFragment on Query {
  productById(id: $id) {
    name
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

