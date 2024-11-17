# Resolve_Sequence_Skip_On_RootField_Fragment_Other_RootField_Selected

## Result

```json
{
  "data": {
    "product": {
      "id": "1",
      "brand": {
        "id": "2",
        "name": "string"
      }
    },
    "other": "string"
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  ... Test @skip(if: $skip)
  other
}

fragment Test on Query {
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
6E1700DC6EE5369F86BDD345970684098CE4EFA2
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { ... Test @skip(if: $skip) other } fragment Test on Query { product { id brand { id name } } }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query Test_1 { product { id brand { id __fusion_exports__1: id } } other }",
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

