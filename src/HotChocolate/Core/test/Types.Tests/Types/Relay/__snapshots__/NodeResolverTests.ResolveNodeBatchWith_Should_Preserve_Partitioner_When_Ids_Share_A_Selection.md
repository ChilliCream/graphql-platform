# ResolveNodeBatchWith_Should_Preserve_Partitioner_When_Ids_Share_A_Selection

## Result

```json
{
  "data": {
    "nodes": [
      {
        "name": "x"
      },
      {
        "name": "y"
      },
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
  "InvocationCount": 2,
  "BatchSizes": [
    2,
    1
  ],
  "ReceivedIds": [
    "x",
    "x",
    "y"
  ]
}
```
