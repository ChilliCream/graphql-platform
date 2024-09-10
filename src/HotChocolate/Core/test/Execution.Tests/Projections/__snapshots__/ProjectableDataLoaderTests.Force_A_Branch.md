# Force_A_Branch

## SQL

```text
-- @__keys_0={ '1' } (DbType = Object)
SELECT p."Name", b."Name", p."Id"
FROM "Products" AS p
INNER JOIN "Brands" AS b ON p."BrandId" = b."Id"
WHERE p."Id" = ANY (@__keys_0)
-- @__keys_0={ '1' } (DbType = Object)
SELECT p."Id", b."Id"
FROM "Products" AS p
INNER JOIN "Brands" AS b ON p."BrandId" = b."Id"
WHERE p."Id" = ANY (@__keys_0)
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

