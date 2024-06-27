# Execute_SpecificationExample_ReturnsExpectedResult

## Query

```graphql
query Example {
  topProducts(filter: { approx: { field: true } })
}
```

## ExpectedFieldCost

```json
8.0
```

## Result

```text
{
  "data": {
    "topProducts": null
  },
  "extensions": {
    "operationCost": {
      "fieldCost": 8,
      "typeCost": 1
    }
  }
}
```

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

