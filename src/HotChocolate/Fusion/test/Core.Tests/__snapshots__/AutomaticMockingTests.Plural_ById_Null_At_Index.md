# Plural_ById_Null_At_Index

## Result

```json
{
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

