# Brand_With_Default_Field_Over_Node

## SQL

```text
-- @__keys_0={ '1' } (DbType = Object)
SELECT b."Id"
FROM "Brands" AS b
WHERE b."Id" = ANY (@__keys_0)
```

## Result

```json
{
  "data": {
    "node": {
      "__typename": "Brand"
    }
  }
}
```

