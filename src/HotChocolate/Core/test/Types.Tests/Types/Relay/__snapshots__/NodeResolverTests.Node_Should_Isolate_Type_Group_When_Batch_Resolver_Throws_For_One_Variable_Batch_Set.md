# Node_Should_Isolate_Type_Group_When_Batch_Resolver_Throws_For_One_Variable_Batch_Set

## Set 0

```json
{
  "variableIndex": 0,
  "data": {
    "node": {
      "name": "x"
    }
  }
}
```

## Set 1

```json
{
  "variableIndex": 1,
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "path": [
        "node"
      ]
    }
  ],
  "data": {
    "node": null
  }
}
```

## Dispatch

```json
{
  "OkInvocationCount": 1,
  "FailingInvocationCount": 1
}
```
