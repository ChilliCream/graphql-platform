# Batch_Should_Complete_When_Batch_Result_Has_Batch_Field_And_Slow_Sibling_Runs

## Result

```json
{
  "data": {
    "parents": [
      {
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
      {
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
    ],
    "slow": "slow"
  }
}
```

## Children batch sizes

```json
[
  2
]
```

## Computed batch sizes

```json
[
  4
]
```
