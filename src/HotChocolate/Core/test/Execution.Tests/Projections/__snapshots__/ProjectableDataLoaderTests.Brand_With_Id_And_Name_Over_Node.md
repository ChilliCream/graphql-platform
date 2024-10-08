# Brand_With_Id_And_Name_Over_Node

## SQL

```text
-- @__keys_0={ '1' } (DbType = Object)
SELECT b."Id", b."Name"
FROM "Brands" AS b
WHERE b."Id" = ANY (@__keys_0)
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

