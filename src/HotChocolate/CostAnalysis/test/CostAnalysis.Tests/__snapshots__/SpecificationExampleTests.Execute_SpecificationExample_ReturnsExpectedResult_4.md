# Execute_SpecificationExample_ReturnsExpectedResult

## Schema

```text
type Query {
    mostPopularProduct(approx: Approximate @cost(weight: "-3.0")): Product
        @cost(weight: "5.0")
}

input Approximate { field: Boolean! }
type Product { field: Boolean! }
```

## Query

```text
query Example {
    mostPopularProduct(approx: { field: true }) { field }

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
    "mostPopularProduct": null,
    "__cost": {
      "requestCosts": {
        "fieldCostByLocation": [
          {
            "path": "Example.mostPopularProduct",
            "cost": 2
          },
          {
            "path": "Example.mostPopularProduct(approx:)",
            "cost": -3
          },
          {
            "path": "Example.mostPopularProduct(approx:).field",
            "cost": 0
          },
          {
            "path": "Example.mostPopularProduct.field",
            "cost": 0
          },
          {
            "path": "Example",
            "cost": 2
          }
        ],
        "fieldCost": 2
      }
    }
  }
}
```

