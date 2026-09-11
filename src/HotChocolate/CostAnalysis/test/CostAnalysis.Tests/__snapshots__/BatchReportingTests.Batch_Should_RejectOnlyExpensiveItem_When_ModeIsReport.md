# Batch_Should_RejectOnlyExpensiveItem_When_ModeIsReport

## ResultCount

```json
2
```

## ExpensiveSet Result:

```text
{
  "variableIndex": 0,
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

## CheapSet

```text
{
  "variableIndex": 1,
  "data": {
    "items": []
  },
  "extensions": {
    "operationCost": {
      "fieldCost": 4,
      "typeCost": 2
    }
  }
}
```
