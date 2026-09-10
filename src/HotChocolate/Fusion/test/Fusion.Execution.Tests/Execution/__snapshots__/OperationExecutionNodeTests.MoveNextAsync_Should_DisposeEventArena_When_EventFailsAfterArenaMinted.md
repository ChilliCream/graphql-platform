# MoveNextAsync_Should_DisposeEventArena_When_EventFailsAfterArenaMinted

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
  "ExceptionMessage": "The subscription event failed after minting an arena.",
  "StreamEnded": true,
  "ArenaRentExceptionType": "ObjectDisposedException",
  "ArenaRentedPageCount": 0
}
```
