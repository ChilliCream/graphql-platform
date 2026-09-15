# Authorize_Should_EnforcePolicy_When_BatchFieldExecutes

## Result

```json
{
  "data": {
    "parents": [
      {
        "value": "secret-1"
      },
      {
        "value": "secret-2"
      },
      {
        "value": "secret-3"
      }
    ]
  }
}
```

## Authorization calls

```json
[
  "Validation"
]
```

## Resolver batches

```json
[
  [
    1,
    2,
    3
  ]
]
```
