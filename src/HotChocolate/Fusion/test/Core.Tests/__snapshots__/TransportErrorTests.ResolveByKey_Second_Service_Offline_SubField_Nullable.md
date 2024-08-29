# ResolveByKey_Second_Service_Offline_SubField_Nullable

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "locations": [
        {
          "line": 5,
          "column": 5
        }
      ],
      "path": [
        "products",
        0,
        "price"
      ]
    },
    {
      "message": "Unexpected Execution Error",
      "locations": [
        {
          "line": 5,
          "column": 5
        }
      ],
      "path": [
        "products",
        1,
        "price"
      ]
    },
    {
      "message": "Unexpected Execution Error",
      "locations": [
        {
          "line": 5,
          "column": 5
        }
      ],
      "path": [
        "products",
        2,
        "price"
      ]
    }
  ],
  "data": {
    "products": [
      {
        "id": "1",
        "name": "string",
        "price": null
      },
      {
        "id": "2",
        "name": "string",
        "price": null
      },
      {
        "id": "3",
        "name": "string",
        "price": null
      }
    ]
  }
}
```

## Request

```graphql
{
  products {
    id
    name
    price
  }
}
```

## QueryPlan Hash

```text
C991588ECF525B8EF311F2923FD2CEE9D7BE5B3A
```

## QueryPlan

```json
{
  "document": "{ products { id name price } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_products_1 { products { id name __fusion_exports__1: id } }",
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
        "document": "query fetch_products_2($__fusion_exports__1: [ID!]!) { productsById(ids: $__fusion_exports__1) { price __fusion_exports__1: id } }",
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

