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
          "displayName": "after"
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
    3
  ]
]
```

## Full array after next

```json
[
  "Alice",
  null,
  "Charlie"
]
```

## Delegate received original array

```json
false
```

## Non-pure children

```json
[
  "after",
  "Alice",
  "Charlie"
]
```
