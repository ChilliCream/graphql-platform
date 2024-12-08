# Multiple_Skip_On_SubFields_Only_Skipped_Fields_Selected

## Request

```graphql
query GetProduct($id: ID!, $skip1: Boolean!, $skip2: Boolean!) {
  productById(id: $id) {
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
      "document": "query($id: ID!, $skip1: Boolean!, $skip2: Boolean!) { productById(id: $id) { name @skip(if: $skip1) description @skip(if: $skip2) } }"
    }
  ]
}
```

