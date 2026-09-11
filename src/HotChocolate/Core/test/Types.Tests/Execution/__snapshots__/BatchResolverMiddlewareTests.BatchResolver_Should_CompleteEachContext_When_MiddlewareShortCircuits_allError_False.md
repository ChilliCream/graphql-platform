# BatchResolver_Should_CompleteEachContext_When_MiddlewareShortCircuits

## Result

```json
{
  "errors": [
    {
      "message": "blocked",
      "path": [
        "users",
        0,
        "profile"
      ]
    },
    {
      "message": "blocked",
      "path": [
        "users",
        1,
        "profile"
      ]
    },
    {
      "message": "blocked",
      "path": [
        "users",
        2,
        "profile"
      ]
    }
  ],
  "data": {
    "users": [
      {
        "profile": null
      },
      {
        "profile": null
      },
      {
        "profile": null
      }
    ]
  }
}
```

## Delegate parents

```json
[]
```

## Full array after next

```json
[
  null,
  null,
  null
]
```

## Delegate received original array

```json
false
```

## Non-pure children

```json
[]
```
