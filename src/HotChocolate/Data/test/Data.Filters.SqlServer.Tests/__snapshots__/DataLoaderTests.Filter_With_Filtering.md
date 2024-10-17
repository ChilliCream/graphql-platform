# Filter_With_Filtering

## SQL

```text
.param set @__keys_0 '[1]'

SELECT "p"."Id", "p"."AvailableStock", "p"."BrandId", "p"."Description", "p"."ImageFileName", "p"."MaxStockThreshold", "p"."Name", "p"."OnReorder", "p"."Price", "p"."RestockThreshold", "p"."TypeId"
FROM "Products" AS "p"
WHERE "p"."BrandId" IN (
    SELECT "k"."value"
    FROM json_each(@__keys_0) AS "k"
)
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

