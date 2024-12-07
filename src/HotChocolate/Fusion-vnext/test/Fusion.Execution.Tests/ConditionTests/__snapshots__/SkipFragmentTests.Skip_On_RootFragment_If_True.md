# Skip_On_RootFragment_If_True

## Request

```graphql
query GetProduct($id: ID!) {
  ... QueryFragment @skip(if: true)
  products {
    nodes {
      name
    }
  }
}

fragment QueryFragment on Query {
  productById(id: $id) {
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
      "document": "{ products { nodes { name } } }"
    }
  ]
}
```

