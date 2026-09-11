# Authorize_Should_PreserveEntryOwnership_When_MiddlewareShortCircuits

## Result

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "parents",
        1,
        "secured"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ],
  "data": {
    "parents": [
      {
        "secured": {
          "id": 1
        }
      },
      {
        "secured": null
      },
      {
        "secured": {
          "id": 3
        }
      }
    ]
  }
}
```

## Authorization calls

```json
[
  "READ:1",
  "READ:2",
  "READ:3"
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

## After-next context counts

```json
[
  3
]
```
