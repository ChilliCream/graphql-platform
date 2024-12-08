# Skip_And_Include_On_SubField_Skip_False

## Request

```graphql
query GetProduct($id: ID!, $include: Boolean!) {
  productById(id: $id) {
    name @include(if: $include) @skip(if: false)
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
      "document": "query($id: ID!, $include: Boolean!) { productById(id: $id) { name @include(if: $include) description } }"
    }
  ]
}
```

