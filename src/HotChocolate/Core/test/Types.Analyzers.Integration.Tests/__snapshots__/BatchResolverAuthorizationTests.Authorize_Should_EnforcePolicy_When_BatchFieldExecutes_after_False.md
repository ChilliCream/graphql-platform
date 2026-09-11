# Authorize_Should_EnforcePolicy_When_BatchFieldExecutes

## Result

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "parents",
        1,
        "value"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ],
  "data": {
    "parents": [
      {
        "value": "secret-1"
      },
      {
        "value": null
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
