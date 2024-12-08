# Skip_On_SubFragment_Only_Skipped_Fragment_Selected_If_True

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    ... ProductFragment @skip(if: true)
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
      "document": "query($id: ID!) { productById(id: $id) { __typename } }"
    }
  ]
}
```

