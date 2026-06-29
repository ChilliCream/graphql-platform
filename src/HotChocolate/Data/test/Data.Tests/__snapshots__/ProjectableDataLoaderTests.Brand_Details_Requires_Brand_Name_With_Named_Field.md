# Brand_Details_Requires_Brand_Name_With_Named_Field

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
      "computedName": "Brand Name:Brand0"
    }
  }
}
```
