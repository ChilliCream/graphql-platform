# Brand_Details_Requires_Brand_Name_With_Proper_Type

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
    "brandById": {
      "details": "Brand Name:Brand0"
    }
  }
}
```

