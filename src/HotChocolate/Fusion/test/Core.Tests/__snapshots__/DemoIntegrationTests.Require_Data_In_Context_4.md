# Require_Data_In_Context_4

## Result

```json
{
  "errors": [
    {
      "message": "The argument `weight` is required.",
      "locations": [
        {
          "line": 1,
          "column": 54
        }
      ],
      "path": [
        "productById"
      ],
      "extensions": {
        "type": "Product",
        "field": "deliveryEstimate",
        "argument": "weight",
        "specifiedBy": "https://spec.graphql.org/October2021/#sec-Required-Arguments"
      }
    },
    {
      "message": "The argument `size` is required.",
      "locations": [
        {
          "line": 1,
          "column": 54
        }
      ],
      "path": [
        "productById"
      ],
      "extensions": {
        "type": "Product",
        "field": "deliveryEstimate",
        "argument": "size",
        "specifiedBy": "https://spec.graphql.org/October2021/#sec-Required-Arguments"
      }
    }
  ],
  "data": {
    "productById": null
  }
}
```

## Request

```graphql
query Requires {
  productById(id: "UHJvZHVjdDox") {
    deliveryEstimate(zip: "12345") {
      min
      max
    }
  }
}
```

## QueryPlan Hash

```text
9234666A9AAB9875F9172451BA52D23F6FB9D270
```

## QueryPlan

```json
{
  "document": "query Requires { productById(id: \u0022UHJvZHVjdDox\u0022) { deliveryEstimate(zip: \u002212345\u0022) { min max } } }",
  "operation": "Requires",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Shipping",
        "document": "query Requires_1 { productById(id: \u0022UHJvZHVjdDox\u0022) { deliveryEstimate(zip: \u002212345\u0022) { min max } } }",
        "selectionSetId": 0
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      }
    ]
  }
}
```

