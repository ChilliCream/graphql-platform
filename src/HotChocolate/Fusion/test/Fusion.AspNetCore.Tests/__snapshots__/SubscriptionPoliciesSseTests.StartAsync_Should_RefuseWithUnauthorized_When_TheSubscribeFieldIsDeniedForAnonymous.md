# StartAsync_Should_RefuseWithUnauthorized_When_TheSubscribeFieldIsDeniedForAnonymous

## Status

```text
401 Unauthorized
```

## Challenge

```text
Bearer
```

## Events

```json
[
  "event: next\n{\n  \"errors\": [\n    {\n      \"message\": \"The current user is not authenticated.\",\n      \"extensions\": {\n        \"code\": \"AUTH_NOT_AUTHENTICATED\"\n      }\n    }\n  ]\n}",
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
  "commits=1 | Subscription.secret @authenticated() Denied unauthenticated | failure=-"
]
```
