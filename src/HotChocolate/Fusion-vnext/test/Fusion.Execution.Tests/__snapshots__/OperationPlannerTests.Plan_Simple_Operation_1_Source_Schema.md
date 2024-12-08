# Plan_Simple_Operation_1_Source_Schema

## Request

```graphql
{
  productById(id: 1) {
    ... Product
  }
}

fragment Product on Product {
  id
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
      "document": "{ productById(id: 1) { id name } }"
    }
  ]
}
```

