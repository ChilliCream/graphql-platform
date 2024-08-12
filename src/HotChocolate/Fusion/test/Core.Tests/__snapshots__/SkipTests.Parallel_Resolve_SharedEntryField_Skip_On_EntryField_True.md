# Parallel_Resolve_SharedEntryField_Skip_On_EntryField

## Result

```json
{
  "data": {}
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  viewer @skip(if: $skip) {
    userId
    name
  }
}
```

## QueryPlan Hash

```text
9B94B894786FE90D76FAB8290F12CD0CBE51C564
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { viewer @skip(if: $skip) { userId name } }",
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
            "document": "query Test_1($skip: Boolean!) { viewer @skip(if: $skip) { name } }",
            "selectionSetId": 0,
            "forwardedVariables": [
              {
                "variable": "skip"
              }
            ]
          },
          {
            "type": "Resolve",
            "subgraph": "Subgraph_2",
            "document": "query Test_2($skip: Boolean!) { viewer @skip(if: $skip) { userId } }",
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

