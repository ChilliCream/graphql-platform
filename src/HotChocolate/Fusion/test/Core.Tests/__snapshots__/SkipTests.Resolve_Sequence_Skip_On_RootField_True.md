# Resolve_Sequence_Skip_On_RootField

## Result

```json
{
  "data": {}
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  product @skip(if: $skip) {
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
D167607A2D21385828EFFCF88ED8194F8B142ECA
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { product @skip(if: $skip) { id brand { id name } } }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query Test_1 { product { id brand { id __fusion_exports__1: id } } }",
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

