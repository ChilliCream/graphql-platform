# Unresolvable_Subgraph_Is_Not_Chosen_If_Data_Is_Available_In_Resolvable_Subgraph

## Result

```json
{
  "data": {
    "viewer": {
      "product": {
        "id": "1",
        "name": "string"
      }
    }
  }
}
```

## Request

```graphql
{
  viewer {
    product {
      id
      name
    }
  }
}
```

## QueryPlan Hash

```text
68F7ACC73F0431E50C4145FBB512E15D04D79198
```

## QueryPlan

```json
{
  "document": "{ viewer { product { id name } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_viewer_1 { viewer { product { id __fusion_exports__1: id } } }",
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
        "document": "query fetch_viewer_2($__fusion_exports__1: ID!) { node(id: $__fusion_exports__1) { ... on Product { name } } }",
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
    "__fusion_exports__1": "Product_id"
  }
}
```

