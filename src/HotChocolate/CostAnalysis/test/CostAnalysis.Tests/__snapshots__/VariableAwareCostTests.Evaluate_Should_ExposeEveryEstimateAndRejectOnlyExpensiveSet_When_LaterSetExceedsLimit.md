# Evaluate_Should_ExposeEveryEstimateAndRejectOnlyExpensiveSet_When_LaterSetExceedsLimit

## CheapSet

```text
{
  "variableIndex": 0,
  "data": {
    "books": {
      "nodes": []
    }
  }
}
```

## ExpensiveSet Result:

```text
{
  "variableIndex": 1,
  "errors": [
    {
      "message": "The maximum allowed type cost was exceeded.",
      "extensions": {
        "code": "HC0047",
        "maxTypeCost": 10,
        "typeCost": 22
      }
    }
  ]
}
```
