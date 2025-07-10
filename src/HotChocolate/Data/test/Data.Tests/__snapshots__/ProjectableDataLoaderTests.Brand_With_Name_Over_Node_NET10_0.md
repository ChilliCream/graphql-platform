# Brand_With_Name_Over_Node

## SQL

```text
-- @keys={ '1' } (DbType = Object)
SELECT b."Name", b."Id"
FROM "Brands" AS b
WHERE b."Id" = ANY (@keys)
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

