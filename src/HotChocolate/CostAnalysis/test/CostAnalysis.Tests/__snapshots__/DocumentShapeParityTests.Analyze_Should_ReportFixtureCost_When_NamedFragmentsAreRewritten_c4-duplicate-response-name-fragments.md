# Analyze_Should_ReportFixtureCost_When_NamedFragmentsAreRewritten

## Raw Named-Fragment Document

```graphql
{
  result {
    ...First
    ...Second
  }
}

fragment First on A {
  label: a
}

fragment Second on A {
  label: a
}
```

## Rewritten Operation.Document

```graphql
{
  result {
    ... on A {
      label: a
    }
  }
}
```

## Costs

```json
{
  "Expected": {
    "TypeCost": 2.0,
    "FieldCost": 11.0
  },
  "NamedRequest": {
    "TypeCost": 2.0,
    "FieldCost": 11.0
  },
  "InlineRequest": {
    "TypeCost": 2.0,
    "FieldCost": 11.0
  },
  "RawCompile": {
    "TypeCost": 2.0,
    "FieldCost": 11.0
  },
  "RewrittenCompile": {
    "TypeCost": 2.0,
    "FieldCost": 11.0
  }
}
```
