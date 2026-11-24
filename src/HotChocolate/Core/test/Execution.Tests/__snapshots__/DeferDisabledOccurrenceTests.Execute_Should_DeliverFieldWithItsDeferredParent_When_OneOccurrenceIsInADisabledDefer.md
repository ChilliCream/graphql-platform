# Execute_Should_DeliverFieldWithItsDeferredParent_When_OneOccurrenceIsInADisabledDefer

## Incremental

```text
{
  "data": {},
  "pending": [
    {
      "id": "2",
      "path": []
    }
  ],
  "incremental": [
    {
      "id": "2",
      "data": {
        "a": {
          "name": "name",
          "x": "x"
        }
      }
    }
  ],
  "completed": [
    {
      "id": "2"
    }
  ],
  "hasNext": false
}

```

## Plain

```json
{
  "data": {
    "a": {
      "name": "name",
      "x": "x"
    }
  }
}
```
