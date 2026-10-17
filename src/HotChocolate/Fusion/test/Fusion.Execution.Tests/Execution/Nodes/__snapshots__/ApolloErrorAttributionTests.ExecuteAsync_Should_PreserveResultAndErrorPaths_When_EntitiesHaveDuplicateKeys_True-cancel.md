# ExecuteAsync_Should_PreserveResultAndErrorPaths_When_EntitiesHaveDuplicateKeys

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
  "ApolloOperationBatchExecutionNode: 2"
]
```

## Canceled Result Disposal

```json
[
  "ObjectDisposedException"
]
```
