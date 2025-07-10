# Brand_With_Id_And_Name_Over_Node

## SQL

```text
-- @keys={ '1' } (DbType = Object)
SELECT b."Id", b."Name"
FROM "Brands" AS b
WHERE b."Id" = ANY (@keys)
```

## Result

```json
{
  "data": {
    "node": {
      "id": "QnJhbmQ6MQ==",
      "name": "Brand0"
    }
  }
}
```

