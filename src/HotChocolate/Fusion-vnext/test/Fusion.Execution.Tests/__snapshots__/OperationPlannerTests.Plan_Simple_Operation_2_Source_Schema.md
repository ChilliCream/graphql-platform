# Plan_Simple_Operation_2_Source_Schema

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
  estimatedDelivery(postCode: "12345")
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
      "document": "{ productById(id: 1) { id name } }",
      "nodes": [
        {
          "kind": "Operation",
          "schema": "SHIPPING",
          "document": "{ productById { estimatedDelivery(postCode: \u002212345\u0022) } }"
        }
      ]
    }
  ]
}
```

