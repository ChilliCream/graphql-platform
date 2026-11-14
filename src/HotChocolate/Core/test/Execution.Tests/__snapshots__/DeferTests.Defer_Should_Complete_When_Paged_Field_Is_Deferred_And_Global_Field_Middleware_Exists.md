# Defer_Should_Complete_When_Paged_Field_Is_Deferred_And_Global_Field_Middleware_Exists

```text
{
  "data": {
    "pets": [
      {
        "name": "p1"
      },
      {
        "name": "p2"
      }
    ]
  },
  "pending": [
    {
      "id": "2",
      "path": [
        "pets",
        0
      ]
    },
    {
      "id": "3",
      "path": [
        "pets",
        1
      ]
    }
  ],
  "incremental": [
    {
      "id": "2",
      "data": {
        "dogs": {
          "nodes": [
            {
              "name": "d1"
            },
            {
              "name": "d2"
            }
          ]
        }
      }
    },
    {
      "id": "3",
      "data": {
        "dogs": {
          "nodes": [
            {
              "name": "d1"
            },
            {
              "name": "d2"
            }
          ]
        }
      }
    }
  ],
  "completed": [
    {
      "id": "2"
    },
    {
      "id": "3"
    }
  ],
  "hasNext": false
}

```
