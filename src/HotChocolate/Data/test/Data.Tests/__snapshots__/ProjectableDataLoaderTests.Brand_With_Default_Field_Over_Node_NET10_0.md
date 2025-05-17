# Brand_With_Default_Field_Over_Node

## SQL

```text
-- @keys={ '1' } (DbType = Object)
SELECT b."Id"
FROM "Brands" AS b
WHERE b."Id" = ANY (@keys)
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

