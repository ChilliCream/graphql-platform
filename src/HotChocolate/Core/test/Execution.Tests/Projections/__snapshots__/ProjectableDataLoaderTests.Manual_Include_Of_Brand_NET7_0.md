# Manual_Include_Of_Brand

## SQL

```text
SELECT p."Name", b."Id", b."DisplayName", b."Name", b."Details_Country_Name", p."Id"
FROM "Products" AS p
INNER JOIN "Brands" AS b ON p."BrandId" = b."Id"
WHERE p."Id" = 1
```

## Result

```json
{
  "data": {
    "productByIdWithBrand": {
      "name": "Product 0-0"
    }
  }
}
```

