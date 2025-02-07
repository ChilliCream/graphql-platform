# Require_Data_In_Context_5

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
    name
    deliveryEstimate(zip: "12345") {
      min
      max
    }
  }
}
```

## QueryPlan Hash

```text
64787216D94A2AE96B22A3ACEFEB98C5A9262F0F
```

## QueryPlan

```json
{
  "document": "query Requires { productById(id: \u0022UHJvZHVjdDox\u0022) { name deliveryEstimate(zip: \u002212345\u0022) { min max } } }",
  "operation": "Requires",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Shipping",
        "document": "query Requires_1 { productById(id: \u0022UHJvZHVjdDox\u0022) { deliveryEstimate(zip: \u002212345\u0022) { min max } __fusion_exports__1: id } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__1"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      },
      {
        "type": "Resolve",
        "subgraph": "Products",
        "document": "query Requires_2($__fusion_exports__1: ID!) { productById(id: $__fusion_exports__1) { name } }",
        "selectionSetId": 1,
        "path": [
          "productById"
        ],
        "requires": [
          {
            "variable": "__fusion_exports__1"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          1
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "Product_id"
  }
}
```

