# Stream_Should_EndWithCompletedErrors_When_SourceFailsAndItemIsNonNullInPropagateMode

```text
{
  "data": {
    "failingSource": [
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
        "failingSource"
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
            "failingSource"
          ]
        }
      ]
    }
  ],
  "hasNext": false
}

```
