# Fixture_Should_ReportExpectedOperationCost_When_Evaluated

## Operation

```text
query {
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

## Expected

```json
{
  "TypeCost": 2.0,
  "FieldCost": 21.0
}
```

## OperationCost

```json
{
  "fieldCost": 21.0,
  "typeCost": 2.0
}
```
