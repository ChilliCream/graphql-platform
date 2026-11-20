# Gateway_Should_ReportTypeNameResult_When_InterfaceObjectIsNotResolvable

## Gateway Result

```json
{
  "errors": [
    {
      "message": "Could not resolve the actual object type from `System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib, Version=11.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Object, System.Private.CoreLib, Version=11.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` for the abstract type `a`.",
      "path": [
        "a"
      ]
    }
  ],
  "data": {
    "a": null
  }
}
```

## Source Requests

```json
[
  {
    "SubgraphName": "a",
    "Body": "{\"query\":\"query Op_8d327ddf_1 { a { __typename } }\"}"
  }
]
```
