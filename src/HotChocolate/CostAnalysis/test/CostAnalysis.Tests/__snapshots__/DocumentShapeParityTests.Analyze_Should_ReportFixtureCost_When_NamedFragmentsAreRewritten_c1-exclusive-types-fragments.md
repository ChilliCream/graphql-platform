# Analyze_Should_ReportFixtureCost_When_NamedFragmentsAreRewritten

## Raw Named-Fragment Document

```graphql
{
  result {
    ...OnA
    ...OnB
  }
}

fragment OnA on A {
  a
}

fragment OnB on B {
  b
}
```

## Rewritten Operation.Document

```graphql
{
  result {
    ... on A {
      a
    }
    ... on B {
      b
    }
  }
}
```

## Costs

```json
{
  "Expected": {
    "TypeCost": 2.0,
    "FieldCost": 21.0
  },
  "NamedRequest": {
    "TypeCost": 2.0,
    "FieldCost": 21.0
  },
  "InlineRequest": {
    "TypeCost": 2.0,
    "FieldCost": 21.0
  },
  "RawCompile": {
    "TypeCost": 2.0,
    "FieldCost": 21.0
  },
  "RewrittenCompile": {
    "TypeCost": 2.0,
    "FieldCost": 21.0
  }
}
```
