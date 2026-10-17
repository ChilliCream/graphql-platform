# ExecuteAsync_Should_PreserveResultAndErrorPaths_When_SourceSuspends

## Result

```json
{
  "errors": [
    {
      "message": "Entity failed.",
      "path": [
        "categories",
        0,
        "title"
      ]
    },
    {
      "message": "Entity failed.",
      "path": [
        "categories",
        1,
        "title"
      ]
    },
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
    ],
    "categories": [
      {
        "title": null
      },
      {
        "title": null
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
