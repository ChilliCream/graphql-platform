# QueryType_Parallel_Multiple_SubGraphs_WithArguments

## Result

```json
{
  "data": {
    "topProducts": [
      {
        "weight": 100,
        "deliveryEstimate": {
          "min": 400,
          "max": 800
        },
        "reviews": [
          {
            "body": "Love it!"
          },
          {
            "body": "Prefer something else."
          }
        ]
      },
      {
        "weight": 1000,
        "deliveryEstimate": {
          "min": 2650,
          "max": 5300
        },
        "reviews": [
          {
            "body": "Too expensive."
          }
        ]
      },
      {
        "weight": 50,
        "deliveryEstimate": {
          "min": 45,
          "max": 90
        },
        "reviews": [
          {
            "body": "Could be better."
          }
        ]
      }
    ]
  }
}
```

## Request

```graphql
query TopProducts {
  topProducts(first: 5) {
    weight
    deliveryEstimate(zip: "12345") {
      min
      max
    }
    reviews {
      body
    }
  }
}
```

## QueryPlan Hash

```text
4AB84DCDE65E48A32B9280B6A7519D9BB425922F
```

## QueryPlan

```json
{
  "document": "query TopProducts { topProducts(first: 5) { weight deliveryEstimate(zip: \u002212345\u0022) { min max } reviews { body } } }",
  "operation": "TopProducts",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Products",
        "document": "query TopProducts_1 { topProducts(first: 5) { weight __fusion_exports__1: dimension { weight } __fusion_exports__2: dimension { size } __fusion_exports__3: id } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__1"
          },
          {
            "variable": "__fusion_exports__2"
          },
          {
            "variable": "__fusion_exports__3"
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
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Shipping",
            "document": "query TopProducts_2($__fusion_exports__1: Int!, $__fusion_exports__2: Int!, $__fusion_exports__3: ID!) { productById(id: $__fusion_exports__3) { deliveryEstimate(weight: $__fusion_exports__1, size: $__fusion_exports__2, zip: \u002212345\u0022) { min max } } }",
            "selectionSetId": 1,
            "path": [
              "productById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__1"
              },
              {
                "variable": "__fusion_exports__2"
              },
              {
                "variable": "__fusion_exports__3"
              }
            ]
          },
          {
            "type": "Resolve",
            "subgraph": "Reviews2",
            "document": "query TopProducts_3($__fusion_exports__3: ID!) { productById(id: $__fusion_exports__3) { reviews { body } } }",
            "selectionSetId": 1,
            "path": [
              "productById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__3"
              }
            ]
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
    "__fusion_exports__1": "Product_dimension_weight",
    "__fusion_exports__2": "Product_dimension_size",
    "__fusion_exports__3": "Product_id"
  }
}
```

