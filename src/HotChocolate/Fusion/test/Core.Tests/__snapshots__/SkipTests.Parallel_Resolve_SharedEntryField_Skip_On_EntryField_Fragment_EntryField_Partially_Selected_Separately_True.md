# Parallel_Resolve_SharedEntryField_Skip_On_EntryField_Fragment_EntryField_Partially_Selected_Separately

## Result

```json
{
  "data": {
    "viewer": {
      "name": "string"
    }
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  ... Test @skip(if: $skip)
  viewer {
    name
  }
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
DE7FCDAA96445C4383E2C3A94814FD94461B9E3E
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { ... Test @skip(if: $skip) viewer { name } } fragment Test on Query { viewer { userId name } }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Subgraph_1",
            "document": "query Test_1 { viewer { name } }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query Test_2 { viewer { userId } }",
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

