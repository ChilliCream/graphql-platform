# Skip_And_Include_On_SubField_Only_Skipped_Field_Selected_Skip_False

## Request

```graphql
query GetProduct($id: ID!, $include: Boolean!) {
  productById(id: $id) {
    name @include(if: $include) @skip(if: false)
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
      "document": "query($id: ID!, $include: Boolean!) { productById(id: $id) { name @include(if: $include) } }"
    }
  ]
}
```

