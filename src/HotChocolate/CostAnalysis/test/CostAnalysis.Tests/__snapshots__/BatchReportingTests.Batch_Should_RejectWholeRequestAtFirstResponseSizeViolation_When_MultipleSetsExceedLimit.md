# Batch_Should_RejectWholeRequestAtFirstResponseSizeViolation_When_MultipleSetsExceedLimit

```text
{
  "errors": [
    {
      "message": "The maximum allowed response size was exceeded.",
      "extensions": {
        "code": "HC0047",
        "maxAllowedResponseSize": 5,
        "maxResponseSize": 11
      }
    }
  ],
  "extensions": {
    "operationCost": {
      "fieldCost": 92,
      "typeCost": 32,
      "maxResponseSize": 11
    }
  }
}
```
