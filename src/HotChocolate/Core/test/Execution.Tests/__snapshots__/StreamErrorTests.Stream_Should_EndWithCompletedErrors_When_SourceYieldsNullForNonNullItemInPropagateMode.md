# Stream_Should_EndWithCompletedErrors_When_SourceYieldsNullForNonNullItemInPropagateMode

```text
{
  "data": {
    "nullYieldingItems": [
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
        "nullYieldingItems"
      ]
    }
  ],
  "completed": [
    {
      "id": "2",
      "errors": [
        {
          "message": "Cannot return null for non-nullable field.",
          "path": [
            "nullYieldingItems",
            1
          ],
          "extensions": {
            "code": "HC0018"
          }
        }
      ]
    }
  ],
  "hasNext": false
}

```
