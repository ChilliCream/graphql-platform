# Skip_And_Include_On_SubField_Skip_False_Include_True

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    name @skip(if: false) @include(if: true)
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
      "document": "query($id: ID!) { productById(id: $id) { name description } }"
    }
  ]
}
```

