# Filter_Query_Should_Work_When_ObjectType_Ignores_Field

## Result

```json
{
  "data": {
    "hiddenNameProductTypes": {
      "nodes": [
        {
          "id": 1
        }
      ]
    }
  }
}
```

## Query 1

```sql
-- @p='1'
-- @p0='11'
SELECT p."Id"
FROM "ProductTypes" AS p
WHERE p."Id" = @p
ORDER BY p."Id"
LIMIT @p0
```
