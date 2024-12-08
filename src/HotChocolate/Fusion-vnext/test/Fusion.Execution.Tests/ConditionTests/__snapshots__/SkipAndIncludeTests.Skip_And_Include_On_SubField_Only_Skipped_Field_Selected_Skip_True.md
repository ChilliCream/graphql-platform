# Skip_And_Include_On_SubField_Only_Skipped_Field_Selected_Skip_True

## Request

```graphql
query GetProduct($id: ID!, $include: Boolean!) {
  productById(id: $id) {
    name @include(if: $include) @skip(if: true)
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
      "document": "query($id: ID!) { productById(id: $id) { __typename } }"
    }
  ]
}
```

