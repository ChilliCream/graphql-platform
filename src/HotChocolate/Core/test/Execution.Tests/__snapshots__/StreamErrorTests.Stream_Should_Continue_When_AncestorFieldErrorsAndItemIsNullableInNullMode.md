# Stream_Should_Continue_When_AncestorFieldErrorsAndItemIsNullableInNullMode

```text
{
  "errors": [
    {
      "message": "ancestor field failed",
      "path": [
        "ancestor",
        "boom"
      ]
    }
  ],
  "data": {
    "ancestor": {
      "nullableItems": [
        {
          "index": 0
        }
      ],
      "boom": null
    }
  },
  "pending": [
    {
      "id": "2",
      "path": [
        "ancestor",
        "nullableItems"
      ]
    }
  ],
  "incremental": [
    {
      "id": "2",
      "items": [
        {
          "index": 1
        },
        {
          "index": 2
        }
      ]
    }
  ],
  "completed": [
    {
      "id": "2"
    }
  ],
  "hasNext": false
}

```
