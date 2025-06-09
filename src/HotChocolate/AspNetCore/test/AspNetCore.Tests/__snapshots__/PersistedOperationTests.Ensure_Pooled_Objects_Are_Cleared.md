# Ensure_Pooled_Objects_Are_Cleared

## Result 1 - Should be OK

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
        "requestedKey": "a73defcdf38e5891e91b9ba532cf4c36"
      }
    }
  ],
  "Extensions": null
}
```

## Result 2 - Should be OK

```json
{
  "ContentType": "application/graphql-response+json; charset=utf-8",
  "StatusCode": "BadRequest",
  "Data": null,
  "Errors": [
    {
      "message": "The query request contains no document or no document id.",
      "extensions": {
        "code": "HC0015"
      }
    }
  ],
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

