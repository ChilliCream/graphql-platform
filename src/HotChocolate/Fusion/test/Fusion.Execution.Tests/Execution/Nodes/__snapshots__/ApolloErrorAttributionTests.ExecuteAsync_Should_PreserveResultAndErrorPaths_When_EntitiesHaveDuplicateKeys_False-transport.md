# ExecuteAsync_Should_PreserveResultAndErrorPaths_When_EntitiesHaveDuplicateKeys

## Result

```json
{
  "errors": [
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
