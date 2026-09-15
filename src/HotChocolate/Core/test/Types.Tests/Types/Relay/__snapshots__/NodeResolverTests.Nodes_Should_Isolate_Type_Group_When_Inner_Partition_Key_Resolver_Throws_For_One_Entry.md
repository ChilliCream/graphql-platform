# Nodes_Should_Isolate_Type_Group_When_Inner_Partition_Key_Resolver_Throws_For_One_Entry

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
  "InvocationCount": 1,
  "BatchSizes": [
    2
  ],
  "ReceivedIds": [
    "x",
    "y"
  ]
}
```
