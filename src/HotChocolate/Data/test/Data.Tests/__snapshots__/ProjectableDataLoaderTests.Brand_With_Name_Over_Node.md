# Brand_With_Name_Over_Node

## SQL

```text
-- @__keys_0={ '1' } (DbType = Object)
SELECT b."Name", b."Id"
FROM "Brands" AS b
WHERE b."Id" = ANY (@__keys_0)
```

## Result

```json
{
  "data": {
    "node": {
      "name": "Brand0"
    }
  }
}
```

