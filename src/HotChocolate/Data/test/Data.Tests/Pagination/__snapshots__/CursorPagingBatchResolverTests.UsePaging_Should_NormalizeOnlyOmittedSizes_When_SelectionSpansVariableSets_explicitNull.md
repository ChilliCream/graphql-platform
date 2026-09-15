# UsePaging_Should_NormalizeOnlyOmittedSizes_When_SelectionSpansVariableSets

## Set 0

```json
{
  "variableIndex": 0,
  "data": {
    "products": {
      "nodes": [
        11,
        12,
        13
      ],
      "pageInfo": {
        "hasNextPage": false,
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
        21,
        22,
        23
      ],
      "pageInfo": {
        "hasNextPage": false,
        "hasPreviousPage": false
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
    1,
    2
  ]
]
```

## Per-entry published and raw arguments

```json
[
  {
    "Id": 1,
    "Raw": null,
    "Published": {
      "First": 10,
      "Last": null,
      "After": null,
      "Before": null
    }
  },
  {
    "Id": 2,
    "Raw": 10,
    "Published": {
      "First": 10,
      "Last": null,
      "After": null,
      "Before": null
    }
  }
]
```
