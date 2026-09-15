# UseProjection_Should_PreserveEntryResults_When_MiddlewareShortCircuits

## Result

```json
{
  "data": {
    "brands": [
      {
        "products": null
      },
      {
        "products": [
          {
            "name": "2-B"
          }
        ]
      }
    ]
  }
}
```

## Resolver batches

```json
[
  [
    2
  ]
]
```

## Middleware widths

```json
[
  2
]
```

## Field errors preserved for outer middleware

```json
[]
```
