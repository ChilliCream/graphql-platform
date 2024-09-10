# ResolveByKey_Skip_On_SubField_Other_Field_Selected

## Result

```json
{
  "data": {
    "products": [
      {
        "id": "1",
        "name": "string",
        "price": 123,
        "other": "string"
      },
      {
        "id": "2",
        "name": "string",
        "price": 123,
        "other": "string"
      },
      {
        "id": "3",
        "name": "string",
        "price": 123,
        "other": "string"
      }
    ]
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  products {
    id
    name
    price @skip(if: $skip)
    other
  }
}
```

## QueryPlan Hash

```text
4ADF4130AAF9816DC72A9671227E2151B3E05554
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { products { id name price @skip(if: $skip) other } }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query Test_1 { products { id name __fusion_exports__1: id } }",
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
        "type": "ResolveByKeyBatch",
        "subgraph": "Subgraph_2",
        "document": "query Test_2($__fusion_exports__1: [ID!]!, $skip: Boolean!) { productsById(ids: $__fusion_exports__1) { price @skip(if: $skip) other __fusion_exports__1: id } }",
        "selectionSetId": 1,
        "path": [
          "productsById"
        ],
        "requires": [
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

