# Batch_Should_Complete_When_One_Async_Parent_Resolver_Throws

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "path": [
        "parents",
        1,
        "children"
      ]
    }
  ],
  "data": {
    "parents": [
      {
        "id": 1,
        "children": [
          {
            "id": 11,
            "computed": "c11"
          },
          {
            "id": 12,
            "computed": "c12"
          }
        ]
      },
      {
        "id": 2,
        "children": null
      }
    ]
  }
}
```
