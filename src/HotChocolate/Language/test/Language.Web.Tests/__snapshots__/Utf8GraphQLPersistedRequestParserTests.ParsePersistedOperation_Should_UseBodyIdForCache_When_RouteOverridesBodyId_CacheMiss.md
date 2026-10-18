# ParsePersistedOperation_Should_UseBodyIdForCache_When_RouteOverridesBodyId

## Final Route Request

```json
{
  "DocumentId": "route-id",
  "OperationName": "Route",
  "Document": "query Body {\n  field\n}",
  "Hash": "63d9a161d7917fdb6ae7a0b37f51e1710de531737012539c1f1e1add19d8d37f",
  "Algorithm": "sha256",
  "HasDocumentBody": true,
  "SameCachedDocument": false
}
```

## Cache And Hash Callbacks

```json
[
  "cache:body-id",
  "hash:span",
  "cache:63d9a161d7917fdb6ae7a0b37f51e1710de531737012539c1f1e1add19d8d37f"
]
```
