# Sort_Query_Should_Work_When_ObjectType_Ignores_Field

## Result

```json
{
  "data": {
    "hiddenNameProductTypes": {
      "nodes": [
        {
          "id": 8
        },
        {
          "id": 7
        },
        {
          "id": 6
        },
        {
          "id": 5
        },
        {
          "id": 4
        },
        {
          "id": 3
        },
        {
          "id": 2
        },
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
-- @__p_0='11'
SELECT p."Id"
FROM "ProductTypes" AS p
ORDER BY p."Id" DESC
LIMIT @__p_0
```
