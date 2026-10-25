# Batch_Should_RejectWholeRequest_When_OneSetExceedsLimit

```text
{
  "errors": [
    {
      "message": "The maximum allowed type cost was exceeded.",
      "extensions": {
        "code": "HC0047",
        "maxTypeCost": 500,
        "typeCost": 1003
      }
    }
  ],
  "extensions": {
    "operationCost": {
      "fieldCost": 3005,
      "typeCost": 1003
    }
  }
}
```
