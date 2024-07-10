# Query_Plan_31_Argument_No_Value_Specified_With_Selection_Set

## UserRequest

```graphql
query Test {
  fieldWithEnumArg {
    test
  }
}
```

## QueryPlan

```json
{
  "document": "query Test { fieldWithEnumArg { test } }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Test",
        "document": "query Test_1 { fieldWithEnumArg { test } }",
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

