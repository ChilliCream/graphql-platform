# Brand_Details_Country_Name

## SQL

```text
-- @keys={ '1' } (DbType = Object)
SELECT b."Name", b."Details_Country_Name" AS "Name", b."Id"
FROM "Brands" AS b
WHERE b."Id" = ANY (@keys)
```

## Result

```json
{
  "data": {
    "brandById": {
      "name": "Brand0",
      "details": {
        "country": {
          "name": "Country0"
        }
      }
    }
  }
}
```

