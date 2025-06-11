# Ensure_Pooled_Objects_Are_Cleared

## Result 1 - Should be OK

```json
{
  "ContentType": "application/graphql-response+json; charset=utf-8",
  "StatusCode": "OK",
  "Data": {
    "hero": {
      "name": "R2-D2"
    }
  },
  "Errors": null,
  "Extensions": null
}
```

## Result 2 - Should be OK

```json
{
  "ContentType": "application/graphql-response+json; charset=utf-8",
  "StatusCode": "OK",
  "Data": {
    "hero": {
      "name": "R2-D2"
    }
  },
  "Errors": null,
  "Extensions": null
}
```

## Result 3 - Should fail

```json
{
  "ContentType": "application/graphql-response+json; charset=utf-8",
  "StatusCode": "BadRequest",
  "Data": null,
  "Errors": [
    {
      "message": "The specified persisted operation key is invalid.",
      "extensions": {
        "code": "HC0020",
        "requestedKey": "ef5c9e7b1e24de261642083d4e294322"
      }
    }
  ],
  "Extensions": null
}
```

