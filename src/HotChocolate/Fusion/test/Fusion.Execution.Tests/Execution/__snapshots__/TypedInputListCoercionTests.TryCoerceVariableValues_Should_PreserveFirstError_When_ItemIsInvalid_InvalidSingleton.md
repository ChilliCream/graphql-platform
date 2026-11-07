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
    "message": "The value `\"wrong\"` is not a valid value for the scalar type `Int`.",
    "extensions": {
      "variable": "ids"
    }
  }
]
```
