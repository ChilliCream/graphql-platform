# Multiple_Skip_On_SubFields

## Request

```graphql
query GetProduct($id: ID!, $skip1: Boolean!, $skip2: Boolean!) {
  productById(id: $id) {
    id
    name @skip(if: $skip1)
    description @skip(if: $skip2)
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
      "document": "query($id: ID!, $skip1: Boolean!, $skip2: Boolean!) { productById(id: $id) { id name @skip(if: $skip1) description @skip(if: $skip2) } }"
    }
  ]
}
```

