# Same_Selection_On_Two_Object_Types_That_Require_Data_From_Another_Subgraph

## Result

```json
{
  "data": {
    "item1": {
      "product": {
        "id": "2",
        "name": "string"
      }
    },
    "item2": {
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
  item1 {
    product {
      id
      name
    }
  }
  item2 {
    product {
      id
      name
    }
  }
}
```

## QueryPlan Hash

```text
E78C88B24F708E43B14A881AD2A993668E068FE2
```

## QueryPlan

```json
{
  "document": "{ item1 { product { id name } } item2 { product { id name } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query fetch_item1_item2_1 { item1 { product { id __fusion_exports__1: id } } item2 { product { id __fusion_exports__2: id } } }",
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
            "document": "query fetch_item1_item2_2($__fusion_exports__1: ID!) { node(id: $__fusion_exports__1) { ... on Product { name } } }",
            "selectionSetId": 3,
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
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query fetch_item1_item2_3($__fusion_exports__2: ID!) { node(id: $__fusion_exports__2) { ... on Product { name } } }",
            "selectionSetId": 4,
            "path": [
              "node"
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
          3,
          4
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "Product_id",
    "__fusion_exports__2": "Product_id"
  }
}
```

