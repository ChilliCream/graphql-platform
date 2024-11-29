# Resolve_Sequence_Node_Second_Service_Offline_SubField_Nullable_Parent_Nullable

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
    "product": {
      "id": "1",
      "brand": {
        "id": "2",
        "name": null
      }
    }
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
D3BBE380CDE08C00EE4F104AAD03C78AC29E4B9C
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
        "document": "query fetch_product_2($__fusion_exports__1: ID!) { node(id: $__fusion_exports__1) { ... on Brand { name } } }",
        "selectionSetId": 2,
        "path": [
          "node"
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

