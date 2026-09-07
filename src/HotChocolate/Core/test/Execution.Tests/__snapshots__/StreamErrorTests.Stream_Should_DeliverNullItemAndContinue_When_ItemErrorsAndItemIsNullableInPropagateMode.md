# Stream_Should_DeliverNullItemAndContinue_When_ItemErrorsAndItemIsNullableInPropagateMode

```text
{
  "data": {
    "nullableItems": [
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
        "nullableItems"
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
          "message": "item field failed",
          "path": [
            "nullableItems",
            1,
            "name"
          ]
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
