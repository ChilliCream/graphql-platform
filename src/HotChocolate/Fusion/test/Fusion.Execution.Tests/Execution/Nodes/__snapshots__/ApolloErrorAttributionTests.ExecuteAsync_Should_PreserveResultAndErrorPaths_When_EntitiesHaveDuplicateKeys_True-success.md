# ExecuteAsync_Should_PreserveResultAndErrorPaths_When_EntitiesHaveDuplicateKeys

## Result

```json
{
  "data": {
    "products": [
      {
        "name": "One"
      },
      {
        "name": "One"
      },
      {
        "name": "Two"
      }
    ],
    "categories": [
      {
        "title": "One"
      },
      {
        "title": "One"
      },
      {
        "title": "Two"
      }
    ]
  }
}
```

## Apollo Dispatches

```json
[
  "ApolloOperationBatchExecutionNode: 2"
]
```
