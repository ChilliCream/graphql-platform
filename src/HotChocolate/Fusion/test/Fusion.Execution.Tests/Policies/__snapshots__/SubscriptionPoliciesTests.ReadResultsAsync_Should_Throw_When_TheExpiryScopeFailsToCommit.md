# ReadResultsAsync_Should_Throw_When_TheExpiryScopeFailsToCommit

## Requested Status Code

```json
"Unauthorized"
```

## Scopes

```json
[
  "commits=1 | Item.name @policy(live) Allowed - | failure=-",
  "commits=1 | Item.name @policy(live) Denied expired | failure=-"
]
```
