# Nodes_Should_Isolate_Partition_When_Inner_Partition_Batch_Resolver_Throws_For_One_Key

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
        1
      ]
    }
  ],
  "data": {
    "nodes": [
      null,
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
