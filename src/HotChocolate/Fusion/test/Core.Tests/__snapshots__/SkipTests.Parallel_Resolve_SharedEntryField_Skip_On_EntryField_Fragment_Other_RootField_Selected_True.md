# Parallel_Resolve_SharedEntryField_Skip_On_EntryField_Fragment_Other_RootField_Selected

## Result

```json
{
  "data": {
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
  viewer {
    userId
    name
  }
}
```

## QueryPlan Hash

```text
52DDC1FC3CEEDF059D4A4A9B40B5682AD44360F4
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { ... Test @skip(if: $skip) other } fragment Test on Query { viewer { userId name } }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query Test_1 { viewer { userId } other }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "Subgraph_1",
            "document": "query Test_2 { viewer { name } }",
            "selectionSetId": 0
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      }
    ]
  }
}
```

