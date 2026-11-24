# Execute_Should_ListParentEntryFirst_When_NestedGroupSettlesBeforeItsParent

## Payload 0

```json
{
  "data": {
    "slowRoot": "root",
    "products": [
      {
        "name": "product"
      }
    ]
  },
  "pending": [
    {
      "id": "2",
      "path": [
        "products",
        0
      ],
      "label": "P"
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
        "slowProduct": "slow product",
        "reviews": [
          {
            "body": "body"
          }
        ]
      }
    },
    {
      "id": "3",
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
