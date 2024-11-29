# Resolve_Sequence_Skip_On_SubField_Other_Field_Selected

## Result

```json
{
  "data": {
    "product": {
      "id": "1",
      "brand": {
        "id": "2",
        "name": "string",
        "other": "string"
      }
    }
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  product {
    id
    brand {
      id
      name @skip(if: $skip)
      other
    }
  }
}
```

## QueryPlan Hash

```text
06ADDD5F781B1908E4503D6617A487BE592DB04D
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { product { id brand { id name @skip(if: $skip) other } } }",
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
        "document": "query Test_2($__fusion_exports__1: ID!, $skip: Boolean!) { brandById(id: $__fusion_exports__1) { name @skip(if: $skip) other } }",
        "selectionSetId": 2,
        "path": [
          "brandById"
        ],
        "requires": [
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

