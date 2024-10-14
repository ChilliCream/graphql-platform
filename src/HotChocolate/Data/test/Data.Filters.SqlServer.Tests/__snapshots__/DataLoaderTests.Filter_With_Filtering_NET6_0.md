# Filter_With_Filtering

## SQL

```text
SELECT "p"."Id", "p"."AvailableStock", "p"."BrandId", "p"."Description", "p"."ImageFileName", "p"."MaxStockThreshold", "p"."Name", "p"."OnReorder", "p"."Price", "p"."RestockThreshold", "p"."TypeId"
FROM "Products" AS "p"
WHERE "p"."BrandId" = 1
```

## Result

```json
{
  "data": {
    "filterContext": [
      {
        "name": "Product 0-0"
      }
    ]
  }
}
```

