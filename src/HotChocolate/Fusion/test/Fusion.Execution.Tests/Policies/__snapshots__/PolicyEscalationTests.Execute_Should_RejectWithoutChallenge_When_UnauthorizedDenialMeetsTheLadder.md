# Execute_Should_RejectWithoutChallenge_When_UnauthorizedDenialMeetsTheLadder

## Response

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ]
}
```

## Transport

```json
{
  "Status": "Forbidden",
  "Challenge": "<absent>"
}
```

## Source Schema Requests

```json
0
```
