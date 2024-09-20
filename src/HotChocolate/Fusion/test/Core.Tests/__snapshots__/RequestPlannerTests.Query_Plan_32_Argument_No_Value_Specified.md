# Query_Plan_32_Argument_No_Value_Specified

## UserRequest

```graphql
query Test {
  fieldWithEnumArg
}
```

## QueryPlan

```json
{
  "document": "query Test { fieldWithEnumArg }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Test",
        "document": "query Test_1 { fieldWithEnumArg }",
        "selectionSetId": 0
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

