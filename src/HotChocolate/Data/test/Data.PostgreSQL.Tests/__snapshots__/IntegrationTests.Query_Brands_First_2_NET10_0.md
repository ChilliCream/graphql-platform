# Query_Brands_First_2

## Result

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "id": "QnJhbmQ6MTE=",
          "name": "Zephyr"
        },
        {
          "id": "QnJhbmQ6MTM=",
          "name": "XE"
        }
      ]
    }
  }
}
```

## Query 1

```sql
-- @p='3'
SELECT b."Id", b."Name"
FROM "Brands" AS b
ORDER BY b."Name" DESC, b."Id"
LIMIT @p
```

