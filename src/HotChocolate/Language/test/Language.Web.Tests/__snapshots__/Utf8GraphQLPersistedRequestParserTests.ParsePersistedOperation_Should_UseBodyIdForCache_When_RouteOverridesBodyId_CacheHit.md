# ParsePersistedOperation_Should_UseBodyIdForCache_When_RouteOverridesBodyId

## Final Route Request

```json
{
  "DocumentId": "route-id",
  "OperationName": "Route",
  "Document": "query Cached {\n  cached\n}",
  "Hash": "cachedhash",
  "Algorithm": "sha256",
  "HasDocumentBody": true,
  "SameCachedDocument": true
}
```

## Cache And Hash Callbacks

```json
[
  "cache:body-id"
]
```
