# Two_Fragments_On_Sub_Selection_With_Different_Selection

## Request

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    ... {
      name
    }
    ... {
      description
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
      "document": "query($id: ID!) { productById(id: $id) { name description } }"
    }
  ]
}
```

