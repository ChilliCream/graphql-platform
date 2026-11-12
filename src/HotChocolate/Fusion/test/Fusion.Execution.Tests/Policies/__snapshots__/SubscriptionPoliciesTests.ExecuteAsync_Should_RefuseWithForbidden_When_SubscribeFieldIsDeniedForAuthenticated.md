# ExecuteAsync_Should_RefuseWithForbidden_When_SubscribeFieldIsDeniedForAuthenticated

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
  "Status": "Forbidden"
}
```

## Source Schema Subscriptions

```json
0
```
