# Query_Plan_35_Argument_Value_Variable_Specified

## UserRequest

```graphql
query Test($variable: TestEnum) {
  fieldWithEnumArg(arg: $variable)
}
```

## QueryPlan

```json
{
  "document": "query Test($variable: TestEnum) { fieldWithEnumArg(arg: $variable) }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Test",
        "document": "query Test_1($variable: TestEnum) { fieldWithEnumArg(arg: $variable) }",
        "selectionSetId": 0,
        "forwardedVariables": [
          {
            "variable": "variable"
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

