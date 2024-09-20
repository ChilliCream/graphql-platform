# Project_And_Do_Not_Project

## SQL

```text
SELECT b."Id", b."DisplayName", b."Name", b."Details_Country_Name"
FROM "Brands" AS b
WHERE b."Id" = 1
SELECT b."Name", b."Id"
FROM "Brands" AS b
WHERE b."Id" = 1
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

