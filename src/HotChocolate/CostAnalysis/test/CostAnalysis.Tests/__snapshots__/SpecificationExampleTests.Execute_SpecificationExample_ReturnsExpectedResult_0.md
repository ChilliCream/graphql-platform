# Execute_SpecificationExample_ReturnsExpectedResult

## Query

```graphql
query Example {
  users(max: 5) {
    age
  }
}
```

## ExpectedFieldCost

```json
11.0
```

## Result

```text
{
  "data": {
    "users": null
  },
  "extensions": {
    "operationCost": {
      "fieldCost": 11,
      "typeCost": 6
    }
  }
}
```

## Schema

```text
type User {
    name: String
    age: Int @cost(weight: "2.0")
}

type Query {
    users(max: Int): [User] @listSize(slicingArguments: ["max"])
}
```

