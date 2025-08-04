# Filter_With_Aliased_Filtering

## SQL

```text
.param set @keys '[1]'
.param set @p_startswith 'Product%'

SELECT "p"."Id", "p"."AvailableStock", "p"."BrandId", "p"."Description", "p"."ImageFileName", "p"."MaxStockThreshold", "p"."Name", "p"."OnReorder", "p"."Price", "p"."RestockThreshold", "p"."TypeId"
FROM "Products" AS "p"
WHERE "p"."BrandId" IN (
    SELECT "k"."value"
    FROM json_each(@keys) AS "k"
) AND "p"."Name" LIKE @p_startswith ESCAPE '\'
.param set @keys '[1]'
.param set @p 'Product 0-0'

SELECT "p"."Id", "p"."AvailableStock", "p"."BrandId", "p"."Description", "p"."ImageFileName", "p"."MaxStockThreshold", "p"."Name", "p"."OnReorder", "p"."Price", "p"."RestockThreshold", "p"."TypeId"
FROM "Products" AS "p"
WHERE "p"."BrandId" IN (
    SELECT "k"."value"
    FROM json_each(@keys) AS "k"
) AND "p"."Name" = @p
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

