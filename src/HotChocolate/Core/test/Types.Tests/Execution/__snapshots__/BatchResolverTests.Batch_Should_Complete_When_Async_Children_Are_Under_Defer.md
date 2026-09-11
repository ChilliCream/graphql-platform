# Batch_Should_Complete_When_Async_Children_Are_Under_Defer

## Stream continuation

```json
[
  true,
  true,
  false
]
```

## Payload

```json
{
  "data": {
    "parents": [
      {
        "id": 1
      },
      {
        "id": 2
      }
    ]
  },
  "pending": [
    {
      "id": "2",
      "path": [
        "parents",
        0
      ]
    },
    {
      "id": "3",
      "path": [
        "parents",
        1
      ]
    }
  ]
}
```

## Payload

```json
{
  "incremental": [
    {
      "id": "2",
      "data": {
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
      }
    }
  ],
  "completed": [
    {
      "id": "2"
    }
  ]
}
```

## Payload

```json
{
  "incremental": [
    {
      "id": "3",
      "data": {
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
  ],
  "completed": [
    {
      "id": "3"
    }
  ]
}
```
