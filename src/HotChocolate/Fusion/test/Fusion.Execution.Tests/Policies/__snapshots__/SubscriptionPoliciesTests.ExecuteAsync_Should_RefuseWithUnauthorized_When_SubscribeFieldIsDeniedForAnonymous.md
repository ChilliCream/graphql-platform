# ExecuteAsync_Should_RefuseWithUnauthorized_When_SubscribeFieldIsDeniedForAnonymous

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
  "Status": "Unauthorized"
}
```

## Source Schema Subscriptions

```json
0
```
