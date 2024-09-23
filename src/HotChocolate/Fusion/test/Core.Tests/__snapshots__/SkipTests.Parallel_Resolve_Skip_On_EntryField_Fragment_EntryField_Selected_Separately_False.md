# Parallel_Resolve_Skip_On_EntryField_Fragment_EntryField_Selected_Separately

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
  }
  ... Test @skip(if: $skip)
}

fragment Test on Query {
  other {
    userId
  }
}
```

## QueryPlan Hash

```text
011B48529F192BCF168898CCD7B79A8D06AC0006
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { viewer { name } other { userId } ... Test @skip(if: $skip) } fragment Test on Query { other { userId } }",
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

