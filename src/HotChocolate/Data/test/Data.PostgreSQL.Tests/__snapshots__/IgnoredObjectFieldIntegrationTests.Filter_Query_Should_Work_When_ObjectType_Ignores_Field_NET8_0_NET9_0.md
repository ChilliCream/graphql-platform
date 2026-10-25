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
-- @__p_0='1'
-- @__p_1='11'
SELECT p."Id"
FROM "ProductTypes" AS p
WHERE p."Id" = @__p_0
ORDER BY p."Id"
LIMIT @__p_1
```
