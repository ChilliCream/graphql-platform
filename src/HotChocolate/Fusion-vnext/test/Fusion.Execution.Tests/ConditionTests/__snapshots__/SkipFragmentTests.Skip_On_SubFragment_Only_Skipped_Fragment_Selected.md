# Skip_On_SubFragment_Only_Skipped_Fragment_Selected

## Request

```graphql
query GetProduct($id: ID!, $skip: Boolean!) {
  productById(id: $id) {
    ... ProductFragment @skip(if: $skip)
  }
}

fragment ProductFragment on Product {
  name
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
      "document": "query($id: ID!, $skip: Boolean!) { productById(id: $id) { ... on Product @skip(if: $skip) { name } } }"
    }
  ]
}
```

