# Error_On_SubField_Skip_On_Preceding_Field_In_Parent

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "locations": [
        {
          "line": 5,
          "column": 7
        }
      ],
      "path": [
        "productById",
        "brand",
        "errorField"
      ]
    }
  ],
  "data": {
    "productById": {
      "brand": {
        "errorField": null
      }
    }
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  productById(id: "1") {
    skippedField @skip(if: $skip)
    brand {
      errorField
    }
  }
}
```

## QueryPlan Hash

```text
7776892690859E1D8B659C68A48BCE681D3F6FDC
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { productById(id: \u00221\u0022) { skippedField @skip(if: $skip) brand { errorField } } }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query Test_1($skip: Boolean!) { productById(id: \u00221\u0022) { skippedField @skip(if: $skip) __fusion_exports__1: id } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__1"
          }
        ],
        "forwardedVariables": [
          {
            "variable": "skip"
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
        "subgraph": "Subgraph_2",
        "document": "query Test_2($__fusion_exports__1: ID!) { productById(id: $__fusion_exports__1) { brand { __fusion_exports__2: id } } }",
        "selectionSetId": 1,
        "path": [
          "productById"
        ],
        "requires": [
          {
            "variable": "__fusion_exports__1"
          }
        ],
        "provides": [
          {
            "variable": "__fusion_exports__2"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          1
        ]
      },
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query Test_3($__fusion_exports__2: ID!) { brandById(id: $__fusion_exports__2) { errorField } }",
        "selectionSetId": 2,
        "path": [
          "brandById"
        ],
        "requires": [
          {
            "variable": "__fusion_exports__2"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          2
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "Product_id",
    "__fusion_exports__2": "Brand_id"
  }
}
```

