# Paging_Should_IsolateEntriesAndNotifyObservers_When_CustomHandlerRuns

## Result

```json
{
  "data": {
    "parents": [
      {
        "products": {
          "items": [
            99
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
  2
]
```

## Observed page items

```json
[
  [
    99
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
