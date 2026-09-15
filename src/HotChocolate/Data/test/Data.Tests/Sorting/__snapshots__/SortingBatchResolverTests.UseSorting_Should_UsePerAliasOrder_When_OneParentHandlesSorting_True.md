# UseSorting_Should_UsePerAliasOrder_When_OneParentHandlesSorting

## Result

```json
{
  "data": {
    "brands": [
      {
        "a": [
          {
            "name": "P1"
          },
          {
            "name": "P2"
          }
        ],
        "b": [
          {
            "name": "P1"
          },
          {
            "name": "P2"
          }
        ]
      },
      {
        "a": [
          {
            "name": "P1"
          },
          {
            "name": "P2"
          }
        ],
        "b": [
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
  ],
  [
    1,
    2
  ]
]
```
