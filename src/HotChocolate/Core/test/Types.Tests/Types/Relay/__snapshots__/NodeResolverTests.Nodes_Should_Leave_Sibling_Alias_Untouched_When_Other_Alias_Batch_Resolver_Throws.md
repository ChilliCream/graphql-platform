# Nodes_Should_Leave_Sibling_Alias_Untouched_When_Other_Alias_Batch_Resolver_Throws

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "path": [
        "a",
        0
      ]
    }
  ],
  "data": {
    "a": [
      null,
      {
        "name": "x"
      }
    ],
    "b": [
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
  "OkInvocationCount": 2,
  "FailingInvocationCount": 1
}
```
