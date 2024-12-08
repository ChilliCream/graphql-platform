# Multiple_Skip_On_SubFields_With_Same_Variable

## Request

```graphql
query GetProduct($id: ID!, $skip: Boolean!) {
  productById(id: $id) {
    id
    name @skip(if: $skip)
    description @skip(if: $skip)
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
      "document": "query($id: ID!, $skip: Boolean!) { productById(id: $id) { id name @skip(if: $skip) description @skip(if: $skip) } }"
    }
  ]
}
```

