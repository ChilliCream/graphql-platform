# ResolveByKey_Skip_On_SubField_Fragment

## Result

```json
{
  "data": {
    "products": [
      {
        "id": "1",
        "name": "string",
        "price": 123
      },
      {
        "id": "2",
        "name": "string",
        "price": 123
      },
      {
        "id": "3",
        "name": "string",
        "price": 123
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
    ... Test @skip(if: $skip)
  }
}

fragment Test on Product {
  price
}
```

## QueryPlan Hash

```text
89B962E6D6970E4B6797A38CC1AE85501E7486CF
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { products { id name ... Test @skip(if: $skip) } } fragment Test on Product { price }",
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
        "document": "query Test_2($__fusion_exports__1: [ID!]!) { productsById(ids: $__fusion_exports__1) { price __fusion_exports__1: id } }",
        "selectionSetId": 1,
        "path": [
          "productsById"
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

