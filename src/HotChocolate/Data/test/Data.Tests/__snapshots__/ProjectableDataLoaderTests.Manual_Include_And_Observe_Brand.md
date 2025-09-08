# Manual_Include_And_Observe_Brand

## SQL

```text
-- @__keys_0={ '1' } (DbType = Object)
SELECT p."Id", p."AvailableStock", p."BrandId", p."Description", p."ImageFileName", p."MaxStockThreshold", p."Name", p."OnReorder", p."Price", p."RestockThreshold", p."TypeId"
FROM "Products" AS p
WHERE p."Id" = ANY (@__keys_0)
```

## Result

```json
{
  "data": {
    "productByIdWithBrandNoSelection": {
      "name": "Product 0-0"
    }
  }
}
```

