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
  "BeforeResolver:parents[0].value:<null>",
  "BeforeResolver:parents[1].value:<null>",
  "BeforeResolver:parents[2].value:<null>"
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
