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
  "A batch resolver must return exactly one result per context. Expected 2 results but got 1.",
  "A batch resolver must return exactly one result per context. Expected 2 results but got 1."
]
```
