# StartAsync_Should_RefuseWithForbidden_When_TheSubscribeFieldIsDeniedForAuthenticated

## Status

```text
403 Forbidden
```

## Challenge

```text
none
```

## Events

```json
[
  "event: next\n{\n  \"errors\": [\n    {\n      \"message\": \"The current user is not authorized to access this resource.\",\n      \"extensions\": {\n        \"code\": \"AUTH_NOT_AUTHORIZED\"\n      }\n    }\n  ]\n}",
  "event: complete"
]
```

## Source Schema Subscriptions

```json
0
```

## Scopes

```json
[
  "commits=1 | Subscription.scoped @requiresScopes() Denied - | failure=-"
]
```
