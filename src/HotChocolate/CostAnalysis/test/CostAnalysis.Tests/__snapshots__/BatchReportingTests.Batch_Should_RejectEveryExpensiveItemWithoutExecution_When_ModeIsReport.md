# Batch_Should_RejectEveryExpensiveItemWithoutExecution_When_ModeIsReport

## ResultCount

```json
2
```

## FirstSet Result:

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

## SecondSet Result:

```text
{
  "variableIndex": 1,
  "errors": [
    {
      "message": "The maximum allowed type cost was exceeded.",
      "extensions": {
        "code": "HC0047",
        "maxTypeCost": 500,
        "typeCost": 601
      }
    }
  ],
  "extensions": {
    "operationCost": {
      "fieldCost": 1801,
      "typeCost": 601
    }
  }
}
```
