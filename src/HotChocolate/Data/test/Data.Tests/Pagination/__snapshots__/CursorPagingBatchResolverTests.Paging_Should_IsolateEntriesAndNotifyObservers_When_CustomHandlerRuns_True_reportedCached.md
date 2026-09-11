# Paging_Should_IsolateEntriesAndNotifyObservers_When_CustomHandlerRuns

## Result

```json
{
  "errors": [
    {
      "message": "reported",
      "path": [
        "parents",
        0,
        "products"
      ]
    }
  ],
  "data": {
    "parents": [
      {
        "products": {
          "items": [
            1
          ]
        }
      },
      {
        "products": {
          "items": [
            2
          ]
        }
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

## Outer middleware widths

```json
[
  2
]
```

## Sliced parents

```json
[
  1,
  2
]
```

## Observed page items

```json
[
  [
    1
  ],
  [
    2
  ]
]
```

## Middleware value preserved

```json
false
```
