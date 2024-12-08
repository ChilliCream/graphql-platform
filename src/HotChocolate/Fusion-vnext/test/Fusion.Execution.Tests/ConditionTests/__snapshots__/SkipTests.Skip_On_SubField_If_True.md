# Skip_On_SubField_If_True

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    name @skip(if: true)
    description
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
      "document": "query($id: ID!) { productById(id: $id) { description } }"
    }
  ]
}
```

