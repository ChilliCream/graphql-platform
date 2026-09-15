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
    3
  ]
]
```
