# Brand_With_Name_Selector_is_Null

## SQL

```text
-- @__keys_0={ '1' } (DbType = Object)
SELECT b."Id", b."DisplayName", b."Name", b."Details_Country_Name"
FROM "Brands" AS b
WHERE b."Id" = ANY (@__keys_0)
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

