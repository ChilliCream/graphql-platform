# UseOffsetPaging_Should_EvaluateCountFlags_When_CompiledSelectionSpansOverflowWords

## Without count

```json
{
  "variableIndex": 0,
  "data": {
    "products": {
      "items": [
        11,
        12,
        13
      ]
    }
  }
}
```

## With count

```json
{
  "variableIndex": 1,
  "data": {
    "products": {
      "items": [
        21,
        22,
        23
      ],
      "__typename": "ProductsCollectionSegment"
    }
  }
}
```

## Reused selection with count

```json
{
  "variableIndex": 0,
  "data": {
    "products": {
      "items": [
        21,
        22,
        23
      ],
      "__typename": "ProductsCollectionSegment"
    }
  }
}
```

## Reused selection without count

```json
{
  "variableIndex": 1,
  "data": {
    "products": {
      "items": [
        11,
        12,
        13
      ]
    }
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
    2,
    1
  ]
]
```
