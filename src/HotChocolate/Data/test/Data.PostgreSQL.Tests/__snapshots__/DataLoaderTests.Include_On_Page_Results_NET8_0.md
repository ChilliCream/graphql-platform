# Include_On_Page_Results

```sql
-- @__brandIds_0={ '1' } (DbType = Object)
SELECT t."BrandId", t0."BrandId", t0."Name", t0."Price", t0."Id"
FROM (
    SELECT p."BrandId"
    FROM "Products" AS p
    WHERE p."BrandId" = ANY (@__brandIds_0)
    GROUP BY p."BrandId"
) AS t
LEFT JOIN (
    SELECT t1."BrandId", t1."Name", t1."Price", t1."Id"
    FROM (
        SELECT p0."BrandId", p0."Name", p0."Price", p0."Id", ROW_NUMBER() OVER(PARTITION BY p0."BrandId" ORDER BY p0."Id") AS row
        FROM "Products" AS p0
        WHERE p0."BrandId" = ANY (@__brandIds_0)
    ) AS t1
    WHERE t1.row <= 6
) AS t0 ON t."BrandId" = t0."BrandId"
ORDER BY t."BrandId", t0."BrandId", t0."Id"
```
