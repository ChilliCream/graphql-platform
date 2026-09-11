# BatchResolver_Should_CompleteEachContext_When_MiddlewareShortCircuits

## Result

```json
{
  "data": {
    "users": [
      {
        "profile": {
          "displayName": "cached"
        }
      },
      {
        "profile": {
          "displayName": "cached"
        }
      },
      {
        "profile": {
          "displayName": "cached"
        }
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
  "cached",
  "cached",
  "cached"
]
```

## Delegate received original array

```json
false
```

## Non-pure children

```json
[
  "cached",
  "cached",
  "cached"
]
```
