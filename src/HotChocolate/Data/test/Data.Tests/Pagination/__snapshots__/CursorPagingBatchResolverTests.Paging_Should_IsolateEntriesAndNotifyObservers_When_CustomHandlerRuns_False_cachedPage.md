# Paging_Should_IsolateEntriesAndNotifyObservers_When_CustomHandlerRuns

## Result

```json
{
  "data": {
    "parents": [
      {
        "products": {
          "nodes": [
            99
          ]
        }
      },
      {
        "products": {
          "nodes": [
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
