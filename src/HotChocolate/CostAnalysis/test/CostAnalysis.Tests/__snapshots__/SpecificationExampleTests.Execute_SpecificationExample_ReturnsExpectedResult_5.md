# Execute_SpecificationExample_ReturnsExpectedResult

## Schema

```text
input Filter {
    approx: Approximate @cost(weight: "-12.0")
}

type Query {
    topProducts(filter: Filter @cost(weight: "15.0")): [String]
        @cost(weight: "5.0") @listSize(assumedSize: 10)
}

input Approximate { field: Boolean! }
```

## Query

```text
query Example {
    topProducts(filter: { approx: { field: true } })

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
    "topProducts": null,
    "__cost": {
      "requestCosts": {
        "fieldCostByLocation": [
          {
            "path": "Example.topProducts",
            "cost": 8
          },
          {
            "path": "Example.topProducts(filter:)",
            "cost": 3
          },
          {
            "path": "Example.topProducts(filter:).approx",
            "cost": -12
          },
          {
            "path": "Example.topProducts(filter:).approx.field",
            "cost": 0
          },
          {
            "path": "Example",
            "cost": 8
          }
        ],
        "fieldCost": 8
      }
    }
  }
}
```

