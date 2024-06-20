# Resolve_Sequence_SubField_NonNull_Parent_NonNull_One_Service_Errors_SubField

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "locations": [
        {
          "line": 6,
          "column": 7
        }
      ],
      "path": [
        "product",
        "brand",
        "name"
      ]
    }
  ],
  "data": {
    "product": null
  }
}
```

## Request

```graphql
{
  product {
    id
    brand {
      id
      name
    }
  }
}
```

## QueryPlan Hash

```text
10F6CB69F78A0E4FD176C0F4651E2E37CF47C9C5
```

## QueryPlan

```json
{
  "document": "{ product { id brand { id name } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_product_1 { product { id brand { id __fusion_exports__1: id } } }",
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
        "subgraph": "Subgraph_2",
        "document": "query fetch_product_2($__fusion_exports__1: ID!) { brandById(id: $__fusion_exports__1) { name } }",
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
        "type": "Compose",
        "selectionSetIds": [
          2
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "Brand_id"
  }
}
```

