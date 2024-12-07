# Skip_And_Include_On_SubField

## Request

```graphql
query GetProduct($id: ID!, $skip: Boolean!, $include: Boolean!) {
  productById(id: $id) {
    name @skip(if: $skip) @include(if: $include)
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
      "document": "query($id: ID!, $include: Boolean!, $skip: Boolean!) { productById(id: $id) { name @skip(if: $skip) @include(if: $include) description } }"
    }
  ]
}
```

