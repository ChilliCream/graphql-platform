# Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_Skip_False_Include_True

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) @skip(if: false) @include(if: true) {
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

