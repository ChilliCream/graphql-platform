# Stream_Should_EndWithCompletedErrors_When_SourceFailsAndItemIsNullableInPropagateMode

```text
{
  "data": {
    "nullableFailingSource": [
      {
        "index": 0,
        "name": "item0"
      }
    ]
  },
  "pending": [
    {
      "id": "2",
      "path": [
        "nullableFailingSource"
      ]
    }
  ],
  "incremental": [
    {
      "id": "2",
      "items": [
        {
          "index": 1,
          "name": "item1"
        }
      ]
    }
  ],
  "completed": [
    {
      "id": "2",
      "errors": [
        {
          "message": "stream source failed",
          "path": [
            "nullableFailingSource"
          ]
        }
      ]
    }
  ],
  "hasNext": false
}

```
