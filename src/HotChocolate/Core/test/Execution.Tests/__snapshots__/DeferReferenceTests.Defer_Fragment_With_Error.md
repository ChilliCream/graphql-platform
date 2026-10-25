# Defer_Fragment_With_Error

```text
{
  "data": {
    "hero": {
      "id": "hero-1"
    }
  },
  "pending": [
    {
      "id": "2",
      "path": [
        "hero"
      ]
    }
  ],
  "incremental": [
    {
      "id": "2",
      "data": {
        "errorField": null
      },
      "errors": [
        {
          "message": "resolver error",
          "path": [
            "hero",
            "errorField"
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
