# Parallel_Resolve_Skip_On_EntryField_Fragment_Other_RootField_Selected

## Result

```json
{
  "data": {
    "viewer": {
      "name": "string"
    },
    "another": "string"
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  viewer {
    name
  }
  ... Test @skip(if: $skip)
  another
}

fragment Test on Query {
  other {
    userId
  }
}
```

## QueryPlan Hash

```text
28FA0CC34DE3FA1E5BEC2B3AA2B722B768B80609
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { viewer { name } ... Test @skip(if: $skip) another } fragment Test on Query { other { userId } }",
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
            "document": "query Test_1 { other { userId } another }",
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

