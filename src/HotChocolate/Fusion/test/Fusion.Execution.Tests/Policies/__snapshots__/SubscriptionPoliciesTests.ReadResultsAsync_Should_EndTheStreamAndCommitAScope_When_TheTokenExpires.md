# ReadResultsAsync_Should_EndTheStreamAndCommitAScope_When_TheTokenExpires

## Event

```text
{
  "data": {
    "changed": {
      "id": "1",
      "name": "n",
      "tag": "t"
    }
  }
}
```

## Has Next

```json
false
```

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
