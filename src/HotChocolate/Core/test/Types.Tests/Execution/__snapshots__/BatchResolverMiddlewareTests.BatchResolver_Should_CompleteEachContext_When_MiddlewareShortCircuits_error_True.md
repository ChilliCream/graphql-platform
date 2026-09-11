# BatchResolver_Should_CompleteEachContext_When_MiddlewareShortCircuits

## Result

```json
{
  "errors": [
    {
      "message": "blocked",
      "path": [
        "users",
        1,
        "profile"
      ]
    }
  ],
  "data": {
    "users": [
      {
        "profile": {
          "displayName": "Alice"
        }
      },
      null,
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
  "Alice",
  "Charlie"
]
```
