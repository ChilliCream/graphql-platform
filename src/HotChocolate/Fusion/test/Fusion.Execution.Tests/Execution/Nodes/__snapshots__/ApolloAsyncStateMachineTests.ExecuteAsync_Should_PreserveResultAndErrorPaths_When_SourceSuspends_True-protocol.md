# ExecuteAsync_Should_PreserveResultAndErrorPaths_When_SourceSuspends

## Result

```json
{
  "errors": [
    {
      "message": "The representation result at '$._entities' must be an array but was 'Object'.",
      "path": [
        "categories",
        0,
        "title"
      ]
    },
    {
      "message": "The representation result at '$._entities' must be an array but was 'Object'.",
      "path": [
        "categories",
        1,
        "title"
      ]
    },
    {
      "message": "The representation result at '$._entities' must be an array but was 'Object'.",
      "path": [
        "categories",
        2,
        "title"
      ]
    },
    {
      "message": "The representation result at '$._entities' must be an array but was 'Object'.",
      "path": [
        "products",
        0,
        "name"
      ]
    },
    {
      "message": "The representation result at '$._entities' must be an array but was 'Object'.",
      "path": [
        "products",
        1,
        "name"
      ]
    },
    {
      "message": "The representation result at '$._entities' must be an array but was 'Object'.",
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
