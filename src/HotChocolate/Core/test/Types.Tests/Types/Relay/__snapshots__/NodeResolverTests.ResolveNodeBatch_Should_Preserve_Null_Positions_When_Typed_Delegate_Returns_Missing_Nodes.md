# ResolveNodeBatch_Should_Preserve_Null_Positions_When_Typed_Delegate_Returns_Missing_Nodes

## Result

```json
{
  "data": {
    "nodes": [
      {
        "name": "x"
      },
      null,
      {
        "name": "x"
      }
    ]
  }
}
```

## Dispatch

```json
{
  "InvocationCount": 1,
  "ReceivedIds": [
    "x",
    "y",
    "x"
  ]
}
```
