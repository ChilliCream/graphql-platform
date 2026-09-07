# Stream_Should_EndWithCompletedErrors_When_ItemErrorsAndItemIsNonNullInPropagateMode

```text
{
  "data": {
    "items": [
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
        "items"
      ]
    }
  ],
  "completed": [
    {
      "id": "2",
      "errors": [
        {
          "message": "item field failed",
          "path": [
            "items",
            1,
            "name"
          ]
        }
      ]
    }
  ],
  "hasNext": false
}

```
