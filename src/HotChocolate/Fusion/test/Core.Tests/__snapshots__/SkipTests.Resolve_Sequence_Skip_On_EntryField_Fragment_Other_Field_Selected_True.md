# Resolve_Sequence_Skip_On_EntryField_Fragment_Other_Field_Selected

## Result

```json
{
  "data": {
    "product": {
      "id": "1",
      "other": "string"
    }
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  product {
    id
    ... Test @skip(if: $skip)
    other
  }
}

fragment Test on Product {
  brand {
    id
    name
  }
}
```

## QueryPlan Hash

```text
D46EDD9E2C2343DBA4034F26AF6A89C30A23A2BE
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { product { id ... Test @skip(if: $skip) other } } fragment Test on Product { brand { id name } }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query Test_1 { product { id brand { id __fusion_exports__1: id } __fusion_exports__2: id } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__1"
          },
          {
            "variable": "__fusion_exports__2"
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
            "subgraph": "Subgraph_2",
            "document": "query Test_2($__fusion_exports__1: ID!) { brandById(id: $__fusion_exports__1) { name } }",
            "selectionSetId": 2,
            "path": [
              "brandById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__1"
              }
            ]
          },
          {
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query Test_3($__fusion_exports__2: ID!) { productById(id: $__fusion_exports__2) { other } }",
            "selectionSetId": 1,
            "path": [
              "productById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__2"
              }
            ]
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          1,
          2
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "Brand_id",
    "__fusion_exports__2": "Product_id"
  }
}
```

