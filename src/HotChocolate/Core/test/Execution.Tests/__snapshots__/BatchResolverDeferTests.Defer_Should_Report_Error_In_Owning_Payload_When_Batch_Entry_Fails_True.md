# Defer_Should_Report_Error_In_Owning_Payload_When_Batch_Entry_Fails

## Payload

```json
{
  "data": {
    "immediate": "ready"
  },
  "pending": [
    {
      "id": "2",
      "path": [],
      "label": "product"
    }
  ],
  "hasNext": true
}
```

## Payload

```json
{
  "completed": [
    {
      "id": "2",
      "errors": [
        {
          "message": "Deferred product failed.",
          "path": [
            "product"
          ]
        }
      ]
    }
  ],
  "hasNext": false
}
```
