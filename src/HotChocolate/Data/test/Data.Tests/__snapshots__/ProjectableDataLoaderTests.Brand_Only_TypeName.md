# Brand_Only_TypeName

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
    "brandById": {
      "__typename": "Brand"
    }
  }
}
```

