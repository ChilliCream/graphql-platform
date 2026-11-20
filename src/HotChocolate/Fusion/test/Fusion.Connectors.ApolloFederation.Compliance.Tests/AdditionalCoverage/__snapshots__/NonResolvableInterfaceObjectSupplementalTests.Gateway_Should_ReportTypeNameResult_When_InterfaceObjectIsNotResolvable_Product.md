# Gateway_Should_ReportTypeNameResult_When_InterfaceObjectIsNotResolvable

## Gateway Result

```json
{
  "data": {
    "product": {
      "__typename": "Bread"
    }
  }
}
```

## Source Requests

```json
[
  {
    "SubgraphName": "a",
    "Body": "{\"query\":\"query Op_e008ce22_1 { product { id } }\"}"
  },
  {
    "SubgraphName": "b",
    "Body": "{\"query\":\"query($representations: [_Any!]!) { _entities(representations: $representations) { ... on Product { __typename } } }\",\"variables\":{\"representations\":[{\"__typename\":\"Product\",\"id\":\"p1\"}]}}"
  }
]
```
