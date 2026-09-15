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
  "Batch resolver must return a list type. Got: HotChocolate.Execution.BatchResolverReproTests+NonListResult.",
  "Batch resolver must return a list type. Got: HotChocolate.Execution.BatchResolverReproTests+NonListResult."
]
```
