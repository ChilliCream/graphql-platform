# Project_And_Do_Not_Project

## SQL

```text
-- @__keys_0={ '1' } (DbType = Object)
SELECT b."Id", b."DisplayName", b."Name", b."Details_Country_Name"
FROM "Brands" AS b
WHERE b."Id" = ANY (@__keys_0)
-- @__keys_0={ '1' } (DbType = Object)
SELECT b."Name", b."Id"
FROM "Brands" AS b
WHERE b."Id" = ANY (@__keys_0)
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

