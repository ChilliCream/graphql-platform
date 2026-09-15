# QueryContext_Should_ProjectUnion_When_BatchMembersHaveOppositeConditions

## Set 0

```json
{
  "variableIndex": 0,
  "data": {
    "product": {
      "id": 1,
      "right": "right"
    }
  }
}
```

## Set 1

```json
{
  "variableIndex": 1,
  "data": {
    "product": {
      "id": 2,
      "left": "left"
    }
  }
}
```

## Skipped set

```json
{
  "variableIndex": 2,
  "data": {}
}
```

## Batch sizes

```json
[
  2
]
```

## Projected before completion

```json
[
  "1:left:right:",
  "2:left:right:"
]
```
