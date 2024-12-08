# Plan_Simple_Operation_1_Source_Schema

## Request

```graphql
{
  productBySlug(slug: "1") {
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
      "document": "{ productBySlug(slug: \u00221\u0022) { id name } }"
    }
  ]
}
```

