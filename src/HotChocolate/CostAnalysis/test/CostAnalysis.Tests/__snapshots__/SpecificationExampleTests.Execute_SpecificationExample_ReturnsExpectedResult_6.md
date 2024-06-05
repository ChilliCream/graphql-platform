# Execute_SpecificationExample_ReturnsExpectedResult

## Schema

```text
directive @approx(tolerance: Float! @cost(weight: "-1.0")) on FIELD

type Query {
    example: [String] @cost(weight: "5.0")
}
```

## Query

```text
query Example {
    example @approx(tolerance: 0)

    __cost {
        requestCosts {
            fieldCostByLocation { path, cost }
            fieldCost
        }
    }
}
```

## Result

```text
{
  "data": {
    "example": null,
    "__cost": {
      "requestCosts": {
        "fieldCostByLocation": [
          {
            "path": "Example.example",
            "cost": 5
          },
          {
            "path": "Example.example.@approx",
            "cost": 0
          },
          {
            "path": "Example.example.@approx(tolerance:)",
            "cost": 0
          },
          {
            "path": "Example",
            "cost": 5
          }
        ],
        "fieldCost": 5
      }
    }
  }
}
```

