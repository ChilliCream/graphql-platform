# Execute_Should_ListEnclosingEntriesFirst_When_ParentGroupSettlesBeforeNestedGroup

## Payload 0

```json
{
  "data": {},
  "pending": [
    {
      "id": "2",
      "path": [],
      "label": "G"
    }
  ],
  "hasNext": true
}
```

## Payload 1

```json
{
  "pending": [
    {
      "id": "3",
      "path": [
        "products",
        0
      ],
      "label": "P"
    },
    {
      "id": "4",
      "path": [
        "products",
        0,
        "reviews",
        0
      ],
      "label": "C"
    }
  ],
  "incremental": [
    {
      "id": "2",
      "data": {
        "slowRoot": "root",
        "products": [
          {
            "name": "product"
          }
        ]
      }
    },
    {
      "id": "3",
      "data": {
        "slowProduct": "slow product",
        "reviews": [
          {
            "body": "body"
          }
        ]
      }
    },
    {
      "id": "4",
      "data": {
        "detail": "detail"
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

## Merged in array order

```json
{
  "slowRoot": "root",
  "products": [
    {
      "name": "product",
      "slowProduct": "slow product",
      "reviews": [
        {
          "body": "body",
          "detail": "detail"
        }
      ]
    }
  ]
}
```
