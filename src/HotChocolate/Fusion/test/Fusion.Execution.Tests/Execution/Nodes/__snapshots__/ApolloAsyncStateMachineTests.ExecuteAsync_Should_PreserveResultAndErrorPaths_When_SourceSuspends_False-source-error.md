# ExecuteAsync_Should_PreserveResultAndErrorPaths_When_SourceSuspends

## Result

```json
{
  "errors": [
    {
      "message": "Entity failed.",
      "path": [
        "products",
        0,
        "name"
      ]
    },
    {
      "message": "Entity failed.",
      "path": [
        "products",
        1,
        "name"
      ]
    }
  ],
  "data": {
    "products": [
      {
        "name": null
      },
      {
        "name": null
      },
      {
        "name": "Two"
      }
    ]
  }
}
```

## Apollo Dispatches

```json
[
  "ApolloOperationExecutionNode"
]
```
