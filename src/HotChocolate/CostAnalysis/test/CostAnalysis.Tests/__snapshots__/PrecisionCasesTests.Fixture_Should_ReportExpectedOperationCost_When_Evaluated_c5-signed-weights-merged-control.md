# Fixture_Should_ReportExpectedOperationCost_When_Evaluated

## Operation

```text
query {
  book {
    title
    author { name }
  }
}

```

## Expected

```json
{
  "TypeCost": 4.0,
  "FieldCost": 2.0
}
```

## OperationCost

```json
{
  "fieldCost": 2.0,
  "typeCost": 4.0
}
```
