# Execute_SpecificationExample_ReturnsExpectedResult

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

## Query

```text
query Example {
    users(max: 5) {
    age
}

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
    "users": null,
    "__cost": {
      "requestCosts": {
        "fieldCostByLocation": [
          {
            "path": "Example.users",
            "cost": 11
          },
          {
            "path": "Example.users(max:)",
            "cost": 0
          },
          {
            "path": "Example.users.age",
            "cost": 10
          },
          {
            "path": "Example",
            "cost": 11
          }
        ],
        "fieldCost": 11
      }
    }
  }
}
```

