# Authorize_Should_EnforceCachedValuePolicy_When_ErrorWasAlreadyReported

## Result

```json
{
  "errors": [
    {
      "message": "recoverable owned error",
      "path": [
        "parents",
        0,
        "secured"
      ]
    },
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "parents",
        0,
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
        "secured": null
      },
      {
        "secured": {
          "id": 2
        }
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
  1,
  2,
  3
]
```

## Resolver dispatches

```json
[
  [
    2,
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
