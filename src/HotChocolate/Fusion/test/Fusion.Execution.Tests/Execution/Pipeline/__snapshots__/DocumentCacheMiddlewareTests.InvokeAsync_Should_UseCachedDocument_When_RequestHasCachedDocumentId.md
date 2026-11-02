# InvokeAsync_Should_UseCachedDocument_When_RequestHasCachedDocumentId

## Result

```json
{
  "data": {
    "__typename": "Query"
  }
}
```

## Document Cache

```json
{
  "AddedToCache": 1,
  "RetrievedFromCache": 1,
  "DocumentInfoAfterCache": [
    {
      "HasDocument": false,
      "IsCached": false,
      "IsValidated": false
    },
    {
      "HasDocument": true,
      "IsCached": true,
      "IsValidated": true
    }
  ]
}
```
