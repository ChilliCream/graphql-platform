# Execute_SpecificationExample_ReturnsExpectedResult

## Schema

```text
type Query {
    topProducts(filter: Filter @cost(weight: "15.0")): [String]
        @cost(weight: "5.0") @listSize(assumedSize: 10)
}

input Filter { field: Boolean! }
```

## Query

```text
query Example {
    topProducts(filter: { field: true })

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
            "cost": 20
          },
          {
            "path": "Example.topProducts(filter:)",
            "cost": 15
          },
          {
            "path": "Example.topProducts(filter:).field",
            "cost": 0
          },
          {
            "path": "Example",
            "cost": 20
          }
        ],
        "fieldCost": 20
      }
    }
  }
}
```

