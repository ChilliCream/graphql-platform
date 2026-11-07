# TryCoerceVariableValues_Should_PreserveFirstError_When_ItemIsInvalid

## Result 1

```json
{
  "Success": false,
  "HasValues": false
}
```

## Result 2

```text
"errors": [
  {
    "message": "The value is not a non-null value.",
    "extensions": {
      "variable": "ids[1][1]"
    }
  }
]
```
