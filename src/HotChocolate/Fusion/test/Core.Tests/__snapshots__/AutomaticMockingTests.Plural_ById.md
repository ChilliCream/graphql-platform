# Plural_ById

## Result

```json
{
  "data": {
    "productsById": [
      {
        "id": "5"
      },
      {
        "id": "6"
      }
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

