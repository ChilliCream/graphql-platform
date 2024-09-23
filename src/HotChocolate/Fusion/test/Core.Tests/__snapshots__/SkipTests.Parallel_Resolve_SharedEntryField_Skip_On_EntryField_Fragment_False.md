# Parallel_Resolve_SharedEntryField_Skip_On_EntryField_Fragment

## Result

```json
{
  "data": {
    "viewer": {
      "userId": "1",
      "name": "string"
    }
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  ... Test @skip(if: $skip)
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
A8CB83116739564244E01F56F4419A743BABD3B4
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { ... Test @skip(if: $skip) } fragment Test on Query { viewer { userId name } }",
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

