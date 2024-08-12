# Parallel_Resolve_SharedEntryField_Skip_On_SubField_Fragment

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
  viewer {
    ... Test @skip(if: $skip)
    name
  }
}

fragment Test on Viewer {
  userId
}
```

## QueryPlan Hash

```text
D71B896CB16891D2B0CA1F6F79ECA60E41F2A41F
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { viewer { ... Test @skip(if: $skip) name } } fragment Test on Viewer { userId }",
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

