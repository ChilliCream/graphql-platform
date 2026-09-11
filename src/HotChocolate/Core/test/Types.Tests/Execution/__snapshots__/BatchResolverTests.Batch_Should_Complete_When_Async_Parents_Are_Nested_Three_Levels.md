# Batch_Should_Complete_When_Async_Parents_Are_Nested_Three_Levels

## Result

```json
{
  "data": {
    "parents": [
      {
        "children": [
          {
            "grandchildren": [
              {
                "id": 111,
                "computed": "g111"
              },
              {
                "id": 112,
                "computed": "g112"
              }
            ]
          },
          {
            "grandchildren": [
              {
                "id": 121,
                "computed": "g121"
              },
              {
                "id": 122,
                "computed": "g122"
              }
            ]
          }
        ]
      },
      {
        "children": [
          {
            "grandchildren": [
              {
                "id": 211,
                "computed": "g211"
              },
              {
                "id": 212,
                "computed": "g212"
              }
            ]
          },
          {
            "grandchildren": [
              {
                "id": 221,
                "computed": "g221"
              },
              {
                "id": 222,
                "computed": "g222"
              }
            ]
          }
        ]
      }
    ]
  }
}
```

## Batch sizes

```json
[
  8
]
```
