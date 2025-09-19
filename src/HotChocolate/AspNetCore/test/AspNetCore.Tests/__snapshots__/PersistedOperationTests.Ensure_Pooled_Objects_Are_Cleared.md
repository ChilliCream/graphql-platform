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
      "message": "Only persisted operations are allowed.",
      "extensions": {
        "code": "HC0067"
      }
    }
  ],
  "Extensions": null
}
```

