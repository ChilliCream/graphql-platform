# Parallel_Resolve_SharedEntryField_Skip_On_SubField

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
    userId @skip(if: $skip)
    name
  }
}
```

## QueryPlan Hash

```text
359E27BB01CBCC1B008A003A40263BBE81CA5A5B
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { viewer { userId @skip(if: $skip) name } }",
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
            "document": "query Test_2($skip: Boolean!) { viewer { userId @skip(if: $skip) } }",
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

