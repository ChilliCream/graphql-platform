# Nodes_Should_Isolate_Type_Group_When_Batch_Resolver_Throws_For_One_Type

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "path": [
        "nodes",
        0
      ]
    },
    {
      "message": "Unexpected Execution Error",
      "path": [
        "nodes",
        2
      ]
    }
  ],
  "data": {
    "nodes": [
      null,
      {
        "name": "x"
      },
      null,
      {
        "name": "y"
      }
    ]
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
