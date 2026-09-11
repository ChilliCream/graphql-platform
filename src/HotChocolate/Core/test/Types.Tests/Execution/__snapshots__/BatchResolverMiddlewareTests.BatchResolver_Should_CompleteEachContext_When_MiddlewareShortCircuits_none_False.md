# BatchResolver_Should_CompleteEachContext_When_MiddlewareShortCircuits

## Result

```json
{
  "data": {
    "users": [
      {
        "profile": {
          "displayName": "Alice"
        }
      },
      {
        "profile": {
          "displayName": "Bob"
        }
      },
      {
        "profile": {
          "displayName": "Charlie"
        }
      }
    ]
  }
}
```

## Delegate parents

```json
[
  [
    1,
    2,
    3
  ]
]
```

## Full array after next

```json
[
  "Alice",
  "Bob",
  "Charlie"
]
```

## Delegate received original array

```json
true
```

## Non-pure children

```json
[
  "Alice",
  "Bob",
  "Charlie"
]
```
