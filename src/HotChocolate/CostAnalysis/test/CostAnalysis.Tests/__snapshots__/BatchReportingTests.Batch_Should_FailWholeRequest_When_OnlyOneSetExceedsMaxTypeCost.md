# Batch_Should_FailWholeRequest_When_OnlyOneSetExceedsMaxTypeCost

```json
{
  "errors": [
    {
      "message": "The maximum allowed type cost was exceeded.",
      "extensions": {
        "code": "HC0047",
        "maxTypeCost": 500,
        "typeCost": 1001
      }
    }
  ],
  "extensions": {
    "operationCost": {
      "fieldCost": 3001,
      "typeCost": 1001
    }
  }
}
```
