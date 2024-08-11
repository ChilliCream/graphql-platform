# Parallel_Resolve_SharedEntryField_Skip_On_EntryField_Other_RootField_Selected

## Result

```json
{
  "data": {
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
C45FAA47967851A4B12625CDD756A1C323C162CE
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
            "document": "query Test_1 { viewer { userId } other }",
            "selectionSetId": 0
          },
          {
            "type": "Resolve",
            "subgraph": "Subgraph_1",
            "document": "query Test_2 { viewer @skip(if: $skip) { name } }",
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

