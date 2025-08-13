# Subgraph_Containing_More_Selections_Is_Chosen

## Result

```json
{
  "data": {
    "productBySlug": {
      "author": {
        "name": "string",
        "rating": 123
      }
    }
  }
}
```

## Request

```graphql
{
  productBySlug {
    author {
      name
      rating
    }
  }
}
```

## QueryPlan Hash

```text
97A88121800B8E021163EA531B156FA2A6CDF11F
```

## QueryPlan

```json
{
  "document": "{ productBySlug { author { name rating } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_productBySlug_1 { productBySlug { __fusion_exports__1: id } }",
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
        "subgraph": "Subgraph_3",
        "document": "query fetch_productBySlug_2($__fusion_exports__1: ID!) { productById(id: $__fusion_exports__1) { author { name rating } } }",
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

