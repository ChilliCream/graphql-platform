# UseSorting_Should_IsolateEntries_When_PostSortingActionFails

## Result

```json
{
  "errors": [
    {
      "message": "Cannot sort this parent's products.",
      "path": [
        "brands",
        0,
        "products"
      ]
    }
  ],
  "data": {
    "brands": [
      {
        "products": null
      },
      {
        "products": [
          {
            "name": "P2"
          },
          {
            "name": "P1"
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
    1,
    2
  ]
]
```

## Per-entry sorting callbacks

```json
[
  "1:True",
  "2:True"
]
```
