# ReceiveAsync_Should_SendTheRefusalAsPerIdError_When_TheTokenExpiredBeforeTheSubscription

## Message

```text
{
  "id": "1",
  "type": "error",
  "payload": [
    {
      "message": "The current user is not authenticated.",
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED"
      }
    }
  ]
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
