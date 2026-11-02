# Deduplicates_List_Fields_In_Initial_Payload

```text
{
  "data": {
    "hero": {
      "friends": [
        {
          "id": "friend-1"
        },
        {
          "id": "friend-2"
        },
        {
          "id": "friend-3"
        }
      ]
    }
  },
  "pending": [
    {
      "id": "2",
      "path": [
        "hero",
        "friends",
        0
      ]
    },
    {
      "id": "3",
      "path": [
        "hero",
        "friends",
        1
      ]
    },
    {
      "id": "4",
      "path": [
        "hero",
        "friends",
        2
      ]
    }
  ],
  "incremental": [
    {
      "id": "2",
      "data": {
        "name": "Han"
      }
    },
    {
      "id": "3",
      "data": {
        "name": "Leia"
      }
    },
    {
      "id": "4",
      "data": {
        "name": "C-3PO"
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
