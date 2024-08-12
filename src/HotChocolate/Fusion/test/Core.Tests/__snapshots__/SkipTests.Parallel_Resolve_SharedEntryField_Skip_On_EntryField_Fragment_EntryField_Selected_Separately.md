# Parallel_Resolve_SharedEntryField_Skip_On_EntryField_Fragment_EntryField_Selected_Separately

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
  viewer {
    userId
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
EC537BBD29F26377D1FE5B7E20A94C4A1038A483
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { ... Test @skip(if: $skip) viewer { userId name } } fragment Test on Query { viewer { userId name } }",
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

