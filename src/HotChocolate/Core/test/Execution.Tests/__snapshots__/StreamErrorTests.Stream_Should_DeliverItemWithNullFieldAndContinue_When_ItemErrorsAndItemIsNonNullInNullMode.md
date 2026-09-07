# Stream_Should_DeliverItemWithNullFieldAndContinue_When_ItemErrorsAndItemIsNonNullInNullMode

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
  "incremental": [
    {
      "id": "2",
      "items": [
        {
          "index": 1,
          "name": null
        },
        {
          "index": 2,
          "name": "item2"
        }
      ],
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
  "completed": [
    {
      "id": "2"
    }
  ],
  "hasNext": false
}

```
