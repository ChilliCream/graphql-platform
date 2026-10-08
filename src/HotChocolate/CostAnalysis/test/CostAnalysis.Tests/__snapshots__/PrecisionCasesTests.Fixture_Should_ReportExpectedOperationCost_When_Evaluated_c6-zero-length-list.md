# Fixture_Should_ReportExpectedOperationCost_When_Evaluated

## Operation

```text
query Zero($n: Int!) {
  items(limit: $n) { value }
}

```

## Expected

```json
{
  "TypeCost": 1.0,
  "FieldCost": 1.0
}
```

## OperationCost

```json
{
  "fieldCost": 1.0,
  "typeCost": 1.0
}
```
