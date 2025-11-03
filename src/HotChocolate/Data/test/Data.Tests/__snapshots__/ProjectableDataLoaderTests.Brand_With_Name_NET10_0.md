# Brand_With_Name

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
    "brandById": {
      "name": "Brand0"
    }
  }
}
```

