# Skip_On_SubField

## Request

```graphql
query GetProduct($id: ID!, $skip: Boolean!) {
  productById(id: $id) {
    name @skip(if: $skip)
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
      "document": "query($id: ID!, $skip: Boolean!) { productById(id: $id) { name @skip(if: $skip) description } }"
    }
  ]
}
```

