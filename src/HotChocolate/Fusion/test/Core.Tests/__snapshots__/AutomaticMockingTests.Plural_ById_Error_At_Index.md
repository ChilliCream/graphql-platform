# Plural_ById_Error_At_Index

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "locations": [
        {
          "line": 2,
          "column": 3
        }
      ],
      "path": [
        "productsById",
        1
      ]
    }
  ],
  "data": {
    "productsById": [
      {
        "id": "5"
      },
      null
    ]
  }
}
```

## Request

```graphql
{
  productsById(ids: [ "5", "6" ]) {
    id
  }
}
```

