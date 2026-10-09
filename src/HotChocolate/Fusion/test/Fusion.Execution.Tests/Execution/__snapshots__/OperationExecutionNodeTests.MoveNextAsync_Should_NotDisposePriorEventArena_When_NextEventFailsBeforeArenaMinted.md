# MoveNextAsync_Should_NotDisposePriorEventArena_When_NextEventFailsBeforeArenaMinted

## Terminal Error Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "path": [
        "onMessage"
      ]
    }
  ],
  "data": {
    "onMessage": null
  }
}
```

## Stream And Arena State

```json
{
  "ExceptionType": "InvalidOperationException",
  "ExceptionMessage": "The next subscription event failed before minting an arena.",
  "StreamEnded": true,
  "FirstEventArenaDisposed": false
}
```
