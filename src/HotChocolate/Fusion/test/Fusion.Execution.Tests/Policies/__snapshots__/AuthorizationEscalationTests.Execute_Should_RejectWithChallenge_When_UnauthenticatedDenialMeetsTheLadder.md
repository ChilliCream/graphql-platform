# Execute_Should_RejectWithChallenge_When_UnauthenticatedDenialMeetsTheLadder

## Response

```json
{
  "errors": [
    {
      "message": "The current user is not authenticated.",
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED"
      }
    }
  ]
}
```

## Transport

```json
{
  "Status": "Unauthorized",
  "Challenge": "Bearer"
}
```

## Source Schema Requests

```json
0
```
