# ExecuteAsync_Should_PreserveResultAndErrorPaths_When_EntitiesHaveDuplicateKeys

## Result

```json
{
  "errors": [
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
