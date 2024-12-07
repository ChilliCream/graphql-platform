# Skip_And_Include_On_SubField_Skip_True_Include_True

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    name @skip(if: true) @include(if: true)
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
      "document": "query($id: ID!) { productById(id: $id) { description } }"
    }
  ]
}
```

