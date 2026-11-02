# ToPageAsync_Should_NotHoistInnerOrderProperties_When_BackwardPagingSelectorContainsNestedOrderBy

## SQL 0

```sql
-- @p='3'
SELECT b."Id", b."Name", (
    SELECT p."Name"
    FROM "Products" AS p
    WHERE b."Id" = p."BrandId"
    ORDER BY p."Price" DESC, p."AvailableStock"
    LIMIT 1) AS "DisplayName"
FROM "Brands" AS b
ORDER BY b."Id" DESC
LIMIT @p
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderByDescending(t => t.Id).Select(root => new Brand() {Id = root.Id, Name = root.Name, DisplayName = root.Products.OrderByDescending(p => p.Price).ThenBy(p => p.AvailableStock).FirstOrDefault().Name}).Take(3)
```
