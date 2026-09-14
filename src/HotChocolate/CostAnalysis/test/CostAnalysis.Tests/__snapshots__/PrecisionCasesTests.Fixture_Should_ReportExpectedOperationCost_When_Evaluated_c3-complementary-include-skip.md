# Fixture_Should_ReportExpectedOperationCost_When_Evaluated

## Operation

```text
query Example($x: Boolean!) {
  left { costly @include(if: $x) }
  right { costly @skip(if: $x) }
}

```

## Expected

```json
{
  "TypeCost": 3.0,
  "FieldCost": 12.0
}
```

## OperationCost

```json
{
  "fieldCost": 12.0,
  "typeCost": 3.0
}
```
