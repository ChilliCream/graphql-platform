# Two_InlineFragments_On_Root_With_Different_Selection

## Request

```graphql
query GetProduct($id: ID!) {
  ... {
    productById(id: $id) {
      name
    }
  }
  ... {
    products {
      nodes {
        description
      }
    }
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
      "document": "query($id: ID!) { productById(id: $id) { name } products { nodes { description } } }"
    }
  ]
}
```

