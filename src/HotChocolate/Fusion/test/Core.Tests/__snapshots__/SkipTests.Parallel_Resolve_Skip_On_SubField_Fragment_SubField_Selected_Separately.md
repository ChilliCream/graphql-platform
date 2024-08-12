# Parallel_Resolve_Skip_On_SubField_Fragment_SubField_Selected_Separately

## Result

```json
{
  "data": {
    "viewer": {
      "name": "string"
    },
    "other": {
      "userId": "1"
    }
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  viewer {
    name
  }
  other {
    userId
    ... Test @skip(if: $skip)
  }
}

fragment Test on Other {
  userId
}
```

## QueryPlan Hash

```text
B9AFD14189FE2CF8604139F95A0501C33D60BB82
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { viewer { name } other { userId ... Test @skip(if: $skip) } } fragment Test on Other { userId }",
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
            "document": "query Test_2 { other { userId } }",
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

