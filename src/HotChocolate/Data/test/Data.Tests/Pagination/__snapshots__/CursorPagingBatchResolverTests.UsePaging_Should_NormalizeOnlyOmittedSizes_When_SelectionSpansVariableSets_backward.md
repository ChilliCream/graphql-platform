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
  "data": {
    "products": {
      "nodes": [
        22,
        23
      ],
      "pageInfo": {
        "hasNextPage": false,
        "hasPreviousPage": true
      }
    }
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
  ],
  [
    2
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
  },
  {
    "Id": 2,
    "Raw": null,
    "Published": {
      "First": null,
      "Last": 2,
      "After": null,
      "Before": null
    }
  }
]
```
