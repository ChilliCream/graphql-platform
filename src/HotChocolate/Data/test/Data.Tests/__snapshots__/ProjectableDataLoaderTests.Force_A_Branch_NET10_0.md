# Force_A_Branch

## SQL

```text
-- @keys={ '1' } (DbType = Object)
SELECT p."Name", FALSE, b."Name", p."Id"
FROM "Products" AS p
INNER JOIN "Brands" AS b ON p."BrandId" = b."Id"
WHERE p."Id" = ANY (@keys)
-- @keys={ '1' } (DbType = Object)
SELECT p."Id", FALSE, b."Id"
FROM "Products" AS p
INNER JOIN "Brands" AS b ON p."BrandId" = b."Id"
WHERE p."Id" = ANY (@keys)
```

## Result

```json
{
  "data": {
    "a": {
      "name": "Product 0-0",
      "brand": {
        "name": "Brand0"
      }
    },
    "b": {
      "id": 1,
      "brand": {
        "id": 1
      }
    }
  }
}
```

