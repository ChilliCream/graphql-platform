# InlineFragment_On_Root_Next_To_Different_Selection

## Request

```graphql
query GetProduct($id: ID!) {
  products {
    nodes {
      description
    }
  }
  ... {
    productById(id: $id) {
      name
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
      "document": "query($id: ID!) { products { nodes { description } } productById(id: $id) { name } }"
    }
  ]
}
```

