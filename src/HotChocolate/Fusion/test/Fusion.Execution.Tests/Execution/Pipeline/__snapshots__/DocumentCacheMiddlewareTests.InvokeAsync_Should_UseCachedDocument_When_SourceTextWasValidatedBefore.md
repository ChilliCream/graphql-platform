# InvokeAsync_Should_UseCachedDocument_When_SourceTextWasValidatedBefore

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
  ],
  "DocumentIdAfterValidation": [
    "ef5c9e7b1e24de261642083d4e294322",
    "ef5c9e7b1e24de261642083d4e294322"
  ]
}
```
