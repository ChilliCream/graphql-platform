# Fixture_Should_ReportExpectedOperationCost_When_Evaluated

## Operation

```text
query {
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

## Expected

```json
{
  "TypeCost": 2.0,
  "FieldCost": 11.0
}
```

## OperationCost

```json
{
  "fieldCost": 11.0,
  "typeCost": 2.0
}
```
