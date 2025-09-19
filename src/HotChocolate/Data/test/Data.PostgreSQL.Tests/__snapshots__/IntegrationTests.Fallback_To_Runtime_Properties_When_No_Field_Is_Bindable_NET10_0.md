# Fallback_To_Runtime_Properties_When_No_Field_Is_Bindable

## Result

```json
{
  "data": {
    "singleProperties": [
      {
        "__typename": "SingleProperty"
      },
      {
        "__typename": "SingleProperty"
      }
    ]
  }
}
```

## Query 1

```sql
SELECT s."Id"
FROM "SingleProperties" AS s
```

