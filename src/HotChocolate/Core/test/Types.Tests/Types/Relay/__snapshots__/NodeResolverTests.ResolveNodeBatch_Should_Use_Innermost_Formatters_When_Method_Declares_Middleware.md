# ResolveNodeBatch_Should_Use_Innermost_Formatters_When_Method_Declares_Middleware

## Result

```json
{
  "data": {
    "nodes": [
      {
        "name": "cached:middleware:directive"
      },
      {
        "name": "y:second:first:middleware:directive"
      },
      {
        "name": "x:second:first:middleware:directive"
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
    "y",
    "x"
  ]
}
```
