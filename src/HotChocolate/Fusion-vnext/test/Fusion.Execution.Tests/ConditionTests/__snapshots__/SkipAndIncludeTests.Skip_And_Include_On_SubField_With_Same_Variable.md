# Skip_And_Include_On_SubField_With_Same_Variable

## Request

```graphql
query GetProduct($id: ID!, $skipOrInclude: Boolean!) {
  productById(id: $id) {
    name @skip(if: $skipOrInclude) @include(if: $skipOrInclude)
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
      "document": "query($id: ID!, $skipOrInclude: Boolean!) { productById(id: $id) { name @skip(if: $skipOrInclude) @include(if: $skipOrInclude) description } }"
    }
  ]
}
```

