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
    topProducts

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
            "cost": 5
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

