# Fixture_Should_ReportExpectedOperationCost_When_Evaluated

## Operation

```text
query Test($n: Int!) {
  results(limit: $n) {
    ... on A { a }
    ... on B { b }
  }
}

```

## Expected

```json
{
  "TypeCost": 5.0,
  "FieldCost": 81.0
}
```

## OperationCost

```json
{
  "fieldCost": 81.0,
  "typeCost": 5.0
}
```
