# Include_On_Page_Results

```sql
-- @__brandIds_0={ '1' } (DbType = Object)
SELECT p1."BrandId", p3."BrandId", p3."Name", p3."Price", p3."Id"
FROM (
    SELECT p."BrandId"
    FROM "Products" AS p
    WHERE p."BrandId" = ANY (@__brandIds_0)
    GROUP BY p."BrandId"
) AS p1
LEFT JOIN (
    SELECT p2."BrandId", p2."Name", p2."Price", p2."Id"
    FROM (
        SELECT p0."BrandId", p0."Name", p0."Price", p0."Id", ROW_NUMBER() OVER(PARTITION BY p0."BrandId" ORDER BY p0."Id") AS row
        FROM "Products" AS p0
        WHERE p0."BrandId" = ANY (@__brandIds_0)
    ) AS p2
    WHERE p2.row <= 6
) AS p3 ON p1."BrandId" = p3."BrandId"
ORDER BY p1."BrandId", p3."BrandId", p3."Id"
```
