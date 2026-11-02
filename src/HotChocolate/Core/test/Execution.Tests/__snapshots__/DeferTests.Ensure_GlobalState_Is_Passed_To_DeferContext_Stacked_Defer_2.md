# Ensure_GlobalState_Is_Passed_To_DeferContext_Stacked_Defer_2

```text
{
  "data": {},
  "pending": [
    {
      "id": "2",
      "path": []
    },
    {
      "id": "3",
      "path": [
        "e"
      ]
    },
    {
      "id": "4",
      "path": [
        "e",
        "more"
      ]
    }
  ],
  "incremental": [
    {
      "id": "2",
      "data": {
        "e": {}
      }
    },
    {
      "id": "3",
      "data": {
        "more": {}
      }
    },
    {
      "id": "4",
      "data": {
        "stuff": "state 123"
      }
    }
  ],
  "completed": [
    {
      "id": "2"
    },
    {
      "id": "3"
    },
    {
      "id": "4"
    }
  ],
  "hasNext": false
}

```
