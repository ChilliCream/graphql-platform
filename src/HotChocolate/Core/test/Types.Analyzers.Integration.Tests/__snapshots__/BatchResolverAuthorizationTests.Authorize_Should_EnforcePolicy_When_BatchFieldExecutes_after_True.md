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
  "AfterResolver:parents[0].value:secret-1",
  "AfterResolver:parents[1].value:secret-2",
  "AfterResolver:parents[2].value:secret-3"
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
