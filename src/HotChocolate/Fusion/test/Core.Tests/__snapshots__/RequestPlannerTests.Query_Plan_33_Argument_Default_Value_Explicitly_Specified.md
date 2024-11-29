# Query_Plan_33_Argument_Default_Value_Explicitly_Specified

## UserRequest

```graphql
query Test {
  fieldWithEnumArg(arg: VALUE2)
}
```

## QueryPlan

```json
{
  "document": "query Test { fieldWithEnumArg(arg: VALUE2) }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Test",
        "document": "query Test_1 { fieldWithEnumArg(arg: VALUE2) }",
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

