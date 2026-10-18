# ParsePersistedOperation_Should_PreserveOverridesAndOwnership_When_BodyHasRequestFields

## Live Request After Input Reuse

```json
{
  "DocumentId": "route-id",
  "OperationName": "Route",
  "ErrorHandlingMode": "Null",
  "HasDocumentBody": false,
  "Variables": "{\"n\":2}",
  "Extensions": "{\"flag\":true}",
  "HasVariablesMemoryOwner": false,
  "HasExtensionsMemoryOwner": false
}
```

## Disposed Documents

```json
{
  "VariablesException": "ObjectDisposedException",
  "ExtensionsException": "ObjectDisposedException"
}
```
