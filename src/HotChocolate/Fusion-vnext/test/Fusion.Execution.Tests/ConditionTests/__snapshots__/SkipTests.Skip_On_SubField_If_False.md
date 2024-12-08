# Skip_On_SubField_If_False

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    name @skip(if: false)
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
      "document": "query($id: ID!) { productById(id: $id) { name description } }"
    }
  ]
}
```

