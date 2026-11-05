# ReadResultsAsync_Should_EndTheStreamWithAnErrorResult_When_TheExpiryScopeFailsToCommit

## Terminal Result

```text
{
  "errors": [
    {
      "message": "Unexpected Execution Error"
    }
  ]
}
```

## Terminal Status Code

```json
"InternalServerError"
```

## Has Next

```json
false
```

## Scopes

```json
[
  "commits=1 | Item.name @policy(live) Allowed - | failure=-",
  "commits=1 | Item.name @policy(live) Denied expired | failure=-"
]
```

## Request Errors

```json
[
  "System.InvalidOperationException: commit"
]
```

## Subscription Event Errors

```json
[]
```
