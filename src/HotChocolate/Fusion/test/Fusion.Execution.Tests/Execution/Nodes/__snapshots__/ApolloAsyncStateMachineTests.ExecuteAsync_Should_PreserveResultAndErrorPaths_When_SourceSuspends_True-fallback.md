# ExecuteAsync_Should_PreserveResultAndErrorPaths_When_SourceSuspends

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "path": [
        "categories",
        0,
        "title"
      ]
    },
    {
      "message": "Unexpected Execution Error",
      "path": [
        "categories",
        1,
        "title"
      ]
    },
    {
      "message": "Unexpected Execution Error",
      "path": [
        "categories",
        2,
        "title"
      ]
    }
  ],
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
        "title": null
      },
      {
        "title": null
      },
      {
        "title": null
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
