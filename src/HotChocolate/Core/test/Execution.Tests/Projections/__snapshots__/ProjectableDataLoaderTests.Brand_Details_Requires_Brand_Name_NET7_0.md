# Brand_Details_Requires_Brand_Name

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
      "details": "Brand Name:Brand0"
    }
  }
}
```

