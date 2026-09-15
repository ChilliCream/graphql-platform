# UsePaging_Should_EvaluateCountFlags_When_CompiledSelectionSpansOverflowWords

## Without count

```json
{
  "variableIndex": 0,
  "data": {
    "products": {
      "nodes": [
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
      "nodes": [
        21,
        22,
        23
      ],
      "__typename": "ProductsConnection"
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
      "nodes": [
        21,
        22,
        23
      ],
      "__typename": "ProductsConnection"
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
      "nodes": [
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
