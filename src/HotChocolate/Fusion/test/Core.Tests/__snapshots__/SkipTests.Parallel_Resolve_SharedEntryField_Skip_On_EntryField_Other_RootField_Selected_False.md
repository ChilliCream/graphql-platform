# Parallel_Resolve_SharedEntryField_Skip_On_EntryField_Other_RootField_Selected

## Result

```json
{
  "data": {
    "viewer": {
      "userId": "1",
      "name": "string"
    },
    "other": "string"
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  viewer @skip(if: $skip) {
    userId
    name
  }
  other
}
```

## QueryPlan Hash

```text
16940BE29715988F4EFF523764D3BB5B7EFD976E
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { viewer @skip(if: $skip) { userId name } other }",
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
            "document": "query Test_1($skip: Boolean!) { viewer @skip(if: $skip) { userId } other }",
            "selectionSetId": 0,
            "forwardedVariables": [
              {
                "variable": "skip"
              }
            ]
          },
          {
            "type": "Resolve",
            "subgraph": "Subgraph_1",
            "document": "query Test_2($skip: Boolean!) { viewer @skip(if: $skip) { name } }",
            "selectionSetId": 0,
            "forwardedVariables": [
              {
                "variable": "skip"
              }
            ]
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

