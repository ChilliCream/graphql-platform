# Query_Node_Brand

## Result

```json
{
  "data": {
    "brand": {
      "id": "QnJhbmQ6MQ==",
      "name": "Daybird"
    }
  }
}
```

## Query 1

```sql
-- @ids={ '1' } (DbType = Object)
SELECT b."Id", b."Name"
FROM "Brands" AS b
WHERE b."Id" = ANY (@ids)
```

