# Branches_Are_Merged

## SQL

```text
-- @__keys_0={ '1', '2' } (DbType = Object)
SELECT p."Name", b."Name", p."Id"
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
      "name": "Product 0-1",
      "brand": {
        "name": "Brand0"
      }
    }
  }
}
```

