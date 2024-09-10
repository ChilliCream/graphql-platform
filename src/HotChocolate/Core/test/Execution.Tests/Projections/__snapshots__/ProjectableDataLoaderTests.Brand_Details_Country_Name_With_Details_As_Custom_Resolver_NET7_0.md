# Brand_Details_Country_Name_With_Details_As_Custom_Resolver

## SQL

```text
SELECT b."Name", b."Id"
FROM "Brands" AS b
WHERE b."Id" = 1
```

## Result

```json
{
  "data": {
    "brandById": {
      "name": "Brand0",
      "details": {
        "country": {
          "name": "Germany"
        }
      }
    }
  }
}
```

