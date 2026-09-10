# AddHttpResponseFormatter_Should_FormatActualResponses_When_UsingAnyOverloadOrBuilderSurface

```text

---
Content-Type: application/json; charset=utf-8

{
  "data": {
    "item": {
      "name": "Item: SXRlbTox"
    }
  },
  "hasNext": true
}
---
Content-Type: application/json; charset=utf-8

{
  "incremental": [
    {
      "data": {
        "field": "Item: SXRlbTox"
      },
      "path": [
        "item"
      ],
      "label": "later"
    }
  ],
  "hasNext": false
}
-----

```
