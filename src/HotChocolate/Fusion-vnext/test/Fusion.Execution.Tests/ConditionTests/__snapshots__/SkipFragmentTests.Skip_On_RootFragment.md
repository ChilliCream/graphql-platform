# Skip_On_RootFragment

## Request

```graphql
query GetProduct($id: ID!, $skip: Boolean!) {
  ... QueryFragment @skip(if: $skip)
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
      "document": "query($id: ID!, $skip: Boolean!) { ... on Query @skip(if: $skip) { productById(id: $id) { name } } products { nodes { name } } }"
    }
  ]
}
```

