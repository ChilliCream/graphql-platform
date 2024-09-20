# Product_With_Name_And_Brand_With_Name

## SQL

```text
-- @__keys_0={ '1' } (DbType = Object)
SELECT p."Name", b."Name", p."Id"
FROM "Products" AS p
INNER JOIN "Brands" AS b ON p."BrandId" = b."Id"
WHERE p."Id" = ANY (@__keys_0)
```

## Result

```json
{
  "data": {
    "productById": {
      "name": "Product 0-0",
      "brand": {
        "name": "Brand0"
      }
    }
  }
}
```

