# Defer_Fragment_With_Errors_On_Top_Level_Query_Field

```text
{
  "data": {},
  "pending": [
    {
      "id": "2",
      "path": []
    }
  ],
  "incremental": [
    {
      "id": "2",
      "data": {
        "a": null
      },
      "errors": [
        {
          "message": "Cannot return null for non-nullable field.",
          "path": [
            "a",
            "nonNullErrorField"
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
