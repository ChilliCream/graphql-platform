# Query_Brands_First_2

## Result

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "id": "QnJhbmQ6OQ==",
          "name": "AirStrider"
        },
        {
          "id": "QnJhbmQ6NQ==",
          "name": "B&R"
        }
      ]
    }
  }
}
```

## Query 1

```sql
-- @__p_0='3'
SELECT b."Id", b."Name"
FROM "Brands" AS b
ORDER BY b."Name", b."Id"
LIMIT @__p_0
```

