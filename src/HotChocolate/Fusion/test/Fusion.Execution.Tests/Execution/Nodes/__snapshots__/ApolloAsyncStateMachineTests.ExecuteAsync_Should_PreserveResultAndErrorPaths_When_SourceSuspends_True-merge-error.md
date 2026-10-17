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
    },
    {
      "message": "Unexpected Execution Error",
      "path": [
        "products",
        0,
        "name"
      ]
    },
    {
      "message": "Unexpected Execution Error",
      "path": [
        "products",
        1,
        "name"
      ]
    },
    {
      "message": "Unexpected Execution Error",
      "path": [
        "products",
        2,
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
        "name": null
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
