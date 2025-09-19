# Execute_SpecificationExample_ReturnsExpectedResult

## Query

```graphql
query Example {
  mostPopularProduct {
    field
  }
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
    "mostPopularProduct": null
  },
  "extensions": {
    "operationCost": {
      "fieldCost": 5,
      "typeCost": 2
    }
  }
}
```

## Schema

```text
type Query {
    mostPopularProduct(approx: Approximate @cost(weight: "-3.0")): Product
        @cost(weight: "5.0")
}

input Approximate { field: Boolean! }
type Product { field: Boolean! }
```

