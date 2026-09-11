# Batch_Should_Complete_When_Serial_Mutation_Parents_Have_Async_Children

## Result

```json
{
  "data": {
    "a": {
      "id": 1,
      "children": [
        {
          "id": 11,
          "computed": "c11"
        },
        {
          "id": 12,
          "computed": "c12"
        }
      ]
    },
    "b": {
      "id": 2,
      "children": [
        {
          "id": 21,
          "computed": "c21"
        },
        {
          "id": 22,
          "computed": "c22"
        }
      ]
    }
  }
}
```

## Batch sizes

```json
[
  2,
  2
]
```

## Events

```json
[
  "mutation-1-start",
  "batch-1-complete",
  "mutation-2-start",
  "batch-2-complete"
]
```
