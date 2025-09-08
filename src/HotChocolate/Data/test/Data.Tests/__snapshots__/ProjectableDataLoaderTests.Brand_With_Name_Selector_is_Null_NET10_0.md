# Brand_With_Name_Selector_is_Null

## SQL

```text
-- @keys={ '1' } (DbType = Object)
SELECT b."Id", b."DisplayName", b."Name", b."Details_Country_Name"
FROM "Brands" AS b
WHERE b."Id" = ANY (@keys)
```

## Result

```json
{
  "data": {
    "brandByIdSelectorNull": {
      "name": "Brand0"
    }
  }
}
```

