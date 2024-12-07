# Skip_And_Include_With_Same_Variable_On_SubField_Only_Skipped_Field_Selected

## Request

```graphql
query GetProduct($id: ID!, $skipOrInclude: Boolean!) {
  productById(id: $id) {
    name @skip(if: $skipOrInclude) @include(if: $skipOrInclude)
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
      "document": "query($id: ID!, $skipOrInclude: Boolean!) { productById(id: $id) { name @skip(if: $skipOrInclude) @include(if: $skipOrInclude) } }"
    }
  ]
}
```

