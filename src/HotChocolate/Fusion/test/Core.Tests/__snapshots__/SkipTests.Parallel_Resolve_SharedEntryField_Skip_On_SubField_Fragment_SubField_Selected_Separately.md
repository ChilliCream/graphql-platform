# Parallel_Resolve_SharedEntryField_Skip_On_SubField_Fragment_SubField_Selected_Separately

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
  viewer {
    ... Test @skip(if: $skip)
    userId
    name
  }
}

fragment Test on Viewer {
  userId
}
```

## QueryPlan Hash

```text
653A2F97D20C9C6C47695F68306379FB45775C7F
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { viewer { ... Test @skip(if: $skip) userId name } } fragment Test on Viewer { userId }",
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

