# Skip_On_SubField_Only_Skipped_Field_Selected

## Request

```graphql
query GetProduct($id: ID!, $skip: Boolean!) {
  productById(id: $id) {
    name @skip(if: $skip)
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
      "document": "query($id: ID!, $skip: Boolean!) { productById(id: $id) { name @skip(if: $skip) } }"
    }
  ]
}
```

