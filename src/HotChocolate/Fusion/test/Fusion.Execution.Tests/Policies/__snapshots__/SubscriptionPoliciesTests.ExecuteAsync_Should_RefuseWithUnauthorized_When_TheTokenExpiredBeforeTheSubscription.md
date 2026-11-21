# ExecuteAsync_Should_RefuseWithUnauthorized_When_TheTokenExpiredBeforeTheSubscription

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

## Scopes

```json
[
  "commits=1 | Item.name @policy(live) Denied unauthenticated | failure=-"
]
```
