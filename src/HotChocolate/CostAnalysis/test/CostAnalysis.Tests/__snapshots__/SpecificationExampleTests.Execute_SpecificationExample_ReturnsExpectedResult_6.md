# Execute_SpecificationExample_ReturnsExpectedResult

## Query

```graphql
query Example {
  example @approx(tolerance: 0)
}
```

## ExpectedFieldCost

```json
5.0
```

## Result

```text
{
  "data": {
    "example": null
  },
  "extensions": {
    "operationCost": {
      "fieldCost": 5,
      "typeCost": 1
    }
  }
}
```

## Schema

```text
directive @approx(tolerance: Float! @cost(weight: "-1.0")) on FIELD

type Query {
    example: [String] @cost(weight: "5.0")
}
```

