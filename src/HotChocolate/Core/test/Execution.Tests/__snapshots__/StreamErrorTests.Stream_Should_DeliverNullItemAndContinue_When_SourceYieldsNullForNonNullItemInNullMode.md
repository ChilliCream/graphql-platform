# Stream_Should_DeliverNullItemAndContinue_When_SourceYieldsNullForNonNullItemInNullMode

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
  "incremental": [
    {
      "id": "2",
      "items": [
        null,
        {
          "index": 2,
          "name": "item2"
        }
      ],
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
  "completed": [
    {
      "id": "2"
    }
  ],
  "hasNext": false
}

```
