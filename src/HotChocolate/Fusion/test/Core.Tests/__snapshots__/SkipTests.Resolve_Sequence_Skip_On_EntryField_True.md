# Resolve_Sequence_Skip_On_EntryField

## Result

```json
{
  "data": {
    "product": {
      "id": "1"
    }
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  product {
    id
    brand @skip(if: $skip) {
      id
      name
    }
  }
}
```

## QueryPlan Hash

```text
766DA2342654C1585E6163852A6CEA388399CB8F
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { product { id brand @skip(if: $skip) { id name } } }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query Test_1($skip: Boolean!) { product { id brand @skip(if: $skip) { id __fusion_exports__1: id } } }",
        "selectionSetId": 0,
        "provides": [
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

