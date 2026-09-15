# UsePaging_Should_NormalizeOnlyOmittedSizes_When_SelectionSpansVariableSets

## Set 0

```json
{
  "variableIndex": 0,
  "data": {
    "products": {
      "nodes": [
        11,
        12
      ],
      "pageInfo": {
        "hasNextPage": true,
        "hasPreviousPage": false
      }
    }
  }
}
```

## Set 1

```json
{
  "variableIndex": 1,
  "errors": [
    {
      "message": "The requested number of values per page must be at least 0.",
      "path": [
        "products"
      ],
      "extensions": {
        "code": "HC0079",
        "coordinate": "Query.products",
        "requestedItems": -1
      }
    }
  ],
  "data": {
    "products": null
  }
}
```

## Invalid set

```json
{
  "variableIndex": 2,
  "errors": [
    {
      "message": "The maximum allowed items per page were exceeded.",
      "path": [
        "products"
      ],
      "extensions": {
        "code": "HC0051",
        "coordinate": "Query.products",
        "requestedItems": 51,
        "maxAllowedItems": 50
      }
    }
  ],
  "data": {
    "products": null
  }
}
```

## Resolver batches

```json
[
  [
    1
  ]
]
```

## Per-entry published and raw arguments

```json
[
  {
    "Id": 1,
    "Raw": 2,
    "Published": {
      "First": 2,
      "Last": null,
      "After": null,
      "Before": null
    }
  }
]
```
