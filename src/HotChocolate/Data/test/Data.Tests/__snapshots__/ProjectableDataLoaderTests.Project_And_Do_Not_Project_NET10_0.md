# Project_And_Do_Not_Project

## SQL

```text
-- @keys={ '1' } (DbType = Object)
SELECT b."Id", b."DisplayName", b."Name", b."Details_Country_Name"
FROM "Brands" AS b
WHERE b."Id" = ANY (@keys)
-- @keys={ '1' } (DbType = Object)
SELECT b."Name", b."Id"
FROM "Brands" AS b
WHERE b."Id" = ANY (@keys)
```

## Result

```json
{
  "data": {
    "brandByIdNoProjection": {
      "name": "Brand0"
    },
    "brandById": {
      "name": "Brand0"
    }
  }
}
```

