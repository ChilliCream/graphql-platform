# Query_Plan_34_Argument_Not_Default_Value_Specified

## UserRequest

```graphql
query Test {
  fieldWithEnumArg(arg: VALUE1)
}
```

## QueryPlan

```json
{
  "document": "query Test { fieldWithEnumArg(arg: VALUE1) }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Test",
        "document": "query Test_1 { fieldWithEnumArg(arg: VALUE1) }",
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

