# Manual_Include_Of_Brand

## SQL

```text
-- @__keys_0={ '1' } (DbType = Object)
SELECT p."Name", b."Id", b."DisplayName", b."Name", b."Details_Country_Name", p."Id"
FROM "Products" AS p
INNER JOIN "Brands" AS b ON p."BrandId" = b."Id"
WHERE p."Id" = ANY (@__keys_0)
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

