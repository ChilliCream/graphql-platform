# Resolve_Sequence_Skip_On_SubField_Fragment_Other_Field_Selected

## Result

```json
{
  "data": {
    "product": {
      "id": "1",
      "brand": {
        "id": "1",
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
      ... Test @skip(if: $skip)
      other
    }
  }
}

fragment Test on Brand {
  name
}
```

## QueryPlan Hash

```text
8D098A71B600717F663C5F09256662A3EF6B7501
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { product { id brand { id ... Test @skip(if: $skip) other } } } fragment Test on Brand { name }",
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
        "document": "query Test_2($__fusion_exports__1: ID!) { brandById(id: $__fusion_exports__1) { name other } }",
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

