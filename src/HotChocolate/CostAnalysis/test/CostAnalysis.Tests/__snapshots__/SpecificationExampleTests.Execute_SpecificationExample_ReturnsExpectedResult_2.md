# Execute_SpecificationExample_ReturnsExpectedResult

## Query

```graphql
query Example {
  topProducts(filter: { field: true })
}
```

## ExpectedFieldCost

```json
20.0
```

## Result

```text
{
  "data": {
    "topProducts": null
  },
  "extensions": {
    "operationCost": {
      "fieldCost": 20,
      "typeCost": 1
    }
  }
}
```

## Schema

```text
type Query {
    topProducts(filter: Filter @cost(weight: "15.0")): [String]
        @cost(weight: "5.0") @listSize(assumedSize: 10)
}

input Filter { field: Boolean! }
```

