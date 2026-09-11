# UseFiltering_Should_PreserveHandledState_When_OneEntryHandlesItsPredicate

## Result

```json
{
  "data": {
    "brands": [
      {
        "products": [
          {
            "name": "P1"
          },
          {
            "name": "P2"
          }
        ]
      },
      {
        "products": [
          {
            "name": "P1"
          }
        ]
      }
    ]
  }
}
```

## Predicates available inside resolver

```json
[
  true,
  true
]
```
