# ExecuteAsync_Should_PreserveResultAndErrorPaths_When_SourceSuspends

## Result

```json
{
  "errors": [
    {
      "message": "The GraphQL request execution was canceled.",
      "extensions": {
        "code": "HC0049"
      }
    }
  ]
}
```

## Apollo Dispatches

```json
[
  "ApolloOperationExecutionNode"
]
```

## Canceled Result Disposal

```json
[
  "ObjectDisposedException"
]
```
