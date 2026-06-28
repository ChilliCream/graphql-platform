# Subscribe_Should_DeliverEachEventWithCrossSchemaData_When_MultipleEvents

```text
{
  "data": {
    "onBookCreated": {
      "id": 1,
      "title": "Foo"
    }
  }
}
---
{
  "data": {
    "onBookCreated": {
      "id": 2,
      "title": "Bar"
    }
  }
}
---
{
  "data": {
    "onBookCreated": {
      "id": 3,
      "title": "Baz"
    }
  }
}
```
