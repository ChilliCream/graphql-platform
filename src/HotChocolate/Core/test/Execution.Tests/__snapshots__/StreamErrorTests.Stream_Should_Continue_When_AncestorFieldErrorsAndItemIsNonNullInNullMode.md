# Stream_Should_Continue_When_AncestorFieldErrorsAndItemIsNonNullInNullMode

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
      "items": [
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
        "items"
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
