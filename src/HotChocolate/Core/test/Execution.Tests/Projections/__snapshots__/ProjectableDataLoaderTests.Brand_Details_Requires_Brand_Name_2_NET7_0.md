# Brand_Details_Requires_Brand_Name_2

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
      "details": "Brand Name:Brand0"
    }
  }
}
```

