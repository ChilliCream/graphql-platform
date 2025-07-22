# Brand_Only_TypeName

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
    "brandById": {
      "__typename": "Brand"
    }
  }
}
```

