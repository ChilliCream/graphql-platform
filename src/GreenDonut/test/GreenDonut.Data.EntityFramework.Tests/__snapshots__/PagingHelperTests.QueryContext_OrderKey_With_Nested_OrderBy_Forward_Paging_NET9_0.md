# QueryContext_OrderKey_With_Nested_OrderBy_Forward_Paging

## SQL 0

```sql
-- @__p_0='3'
SELECT b0."Id", b0."Name", p0."Id", p0."AvailableStock", p0."BrandId", p0."Description", p0."ImageFileName", p0."MaxStockThreshold", p0."Name", p0."OnReorder", p0."Price", p0."RestockThreshold", p0."TypeId"
FROM (
    SELECT b."Id", b."Name", (
        SELECT p."Price"
        FROM "Products" AS p
        WHERE b."Id" = p."BrandId"
        ORDER BY p."Price"
        LIMIT 1) AS c
    FROM "Brands" AS b
    ORDER BY (
        SELECT p."Price"
        FROM "Products" AS p
        WHERE b."Id" = p."BrandId"
        ORDER BY p."Price"
        LIMIT 1), b."Id"
    LIMIT @__p_0
) AS b0
LEFT JOIN "Products" AS p0 ON b0."Id" = p0."BrandId"
ORDER BY b0.c, b0."Id"
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Products.OrderBy(p => p.Price).First().Price).ThenBy(t => t.Id).Select(t => new Brand() {Id = t.Id, Name = t.Name, Products = t.Products}).Take(3)
```
