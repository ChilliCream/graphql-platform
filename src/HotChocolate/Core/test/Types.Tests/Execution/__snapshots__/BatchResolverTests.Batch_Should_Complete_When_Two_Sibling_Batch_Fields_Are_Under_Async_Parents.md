# Batch_Should_Complete_When_Two_Sibling_Batch_Fields_Are_Under_Async_Parents

## Result

```json
{
  "data": {
    "parents": [
      {
        "children": [
          {
            "id": 11,
            "computedA": "a11",
            "computedB": "b11"
          },
          {
            "id": 12,
            "computedA": "a12",
            "computedB": "b12"
          }
        ]
      },
      {
        "children": [
          {
            "id": 21,
            "computedA": "a21",
            "computedB": "b21"
          },
          {
            "id": 22,
            "computedA": "a22",
            "computedB": "b22"
          }
        ]
      }
    ]
  }
}
```

## Batch A sizes

```json
[
  4
]
```

## Batch B sizes

```json
[
  4
]
```
