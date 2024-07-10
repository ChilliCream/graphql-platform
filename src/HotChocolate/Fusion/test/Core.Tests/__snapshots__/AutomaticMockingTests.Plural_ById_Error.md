# Plural_ById_Error

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
        "productsById"
      ]
    }
  ],
  "data": {
    "productsById": null
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

