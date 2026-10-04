# Batch_Should_RejectWholeRequest_When_SummedCostExceedsLimit

```text
{
  "errors": [
    {
      "message": "The maximum allowed type cost was exceeded.",
      "extensions": {
        "code": "HC0047",
        "maxTypeCost": 150,
        "typeCost": 202
      }
    }
  ],
  "extensions": {
    "operationCost": {
      "fieldCost": 602,
      "typeCost": 202
    }
  }
}
```
