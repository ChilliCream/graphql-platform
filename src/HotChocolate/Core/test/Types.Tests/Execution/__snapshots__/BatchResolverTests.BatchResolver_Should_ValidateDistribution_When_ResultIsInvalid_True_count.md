# BatchResolver_Should_ValidateDistribution_When_ResultIsInvalid

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "path": [
        "users",
        0,
        "value"
      ]
    },
    {
      "message": "Unexpected Execution Error",
      "path": [
        "users",
        1,
        "value"
      ]
    }
  ],
  "data": {
    "users": [
      {
        "value": null
      },
      {
        "value": null
      }
    ]
  }
}
```

## Failure

```json
[
  "List count failed.",
  "List count failed."
]
```
