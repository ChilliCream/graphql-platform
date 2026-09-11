# BatchPipeline_Should_SkipResolvedContexts_When_NoMiddlewareConfigured

## Result

```json
{
  "errors": [
    {
      "message": "blocked",
      "path": [
        "users",
        2,
        "name"
      ]
    }
  ],
  "data": {
    "users": [
      {
        "name": "Alice"
      },
      {
        "name": "cached"
      },
      {
        "name": null
      }
    ]
  }
}
```

## Bare delegate parents

```json
[
  [
    1
  ]
]
```
