# ToPageAsync_Should_CreateCursor_When_SelectorContainsNestedOrderBy

## SQL 0

```sql
-- @__p_0='3'
SELECT b."Id", b."Name", (
    SELECT p."Name"
    FROM "Products" AS p
    WHERE b."Id" = p."BrandId"
    ORDER BY p."Price" DESC, p."AvailableStock"
    LIMIT 1) AS "DisplayName"
FROM "Brands" AS b
ORDER BY b."Id"
LIMIT @__p_0
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Id).Select(root => new Brand() {Id = root.Id, Name = root.Name, DisplayName = root.Products.OrderByDescending(p => p.Price).ThenBy(p => p.AvailableStock).FirstOrDefault().Name}).Take(3)
```
