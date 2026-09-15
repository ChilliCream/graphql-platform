# UseFiltering_Should_IsolateEntries_When_OneFilterFails

## Result

```json
{
  "errors": [
    {
      "message": "The provided value for filter `contains` of type StringOperationFilterInput is invalid. Null values are not supported.",
      "locations": [
        {
          "line": 1,
          "column": 21
        }
      ],
      "path": [
        "brands",
        0,
        "products"
      ],
      "extensions": {
        "code": "HC0026",
        "expectedType": "String!",
        "filterType": "StringOperationFilterInput"
      }
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

## Middleware widths

```json
[
  2
]
```
