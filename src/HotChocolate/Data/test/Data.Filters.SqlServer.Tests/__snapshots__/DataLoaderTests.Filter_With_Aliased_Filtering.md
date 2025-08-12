# Filter_With_Aliased_Filtering

## SQL

```text
.param set @__keys_0 '[1]'
.param set @__p_1_rewritten 'Product%'

SELECT "p"."Id", "p"."AvailableStock", "p"."BrandId", "p"."Description", "p"."ImageFileName", "p"."MaxStockThreshold", "p"."Name", "p"."OnReorder", "p"."Price", "p"."RestockThreshold", "p"."TypeId"
FROM "Products" AS "p"
WHERE "p"."BrandId" IN (
    SELECT "k"."value"
    FROM json_each(@__keys_0) AS "k"
) AND "p"."Name" LIKE @__p_1_rewritten ESCAPE '\'
.param set @__keys_0 '[1]'
.param set @__p_1 'Product 0-0'

SELECT "p"."Id", "p"."AvailableStock", "p"."BrandId", "p"."Description", "p"."ImageFileName", "p"."MaxStockThreshold", "p"."Name", "p"."OnReorder", "p"."Price", "p"."RestockThreshold", "p"."TypeId"
FROM "Products" AS "p"
WHERE "p"."BrandId" IN (
    SELECT "k"."value"
    FROM json_each(@__keys_0) AS "k"
) AND "p"."Name" = @__p_1
```

## Result

```json
{
  "data": {
    "a": [
      {
        "name": "Product 0-0"
      }
    ],
    "b": [
      {
        "name": "Product 0-0"
      }
    ]
  }
}
```

