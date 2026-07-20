# ToPageAsync_Should_CreateCursor_When_PredicateContainsNestedOrderBy

## SQL 0

```sql
-- @p='3'
SELECT b."Id", b."Name"
FROM "Brands" AS b
WHERE (
    SELECT p."Price"
    FROM "Products" AS p
    WHERE b."Id" = p."BrandId"
    ORDER BY p."Price" DESC
    LIMIT 1) >= 0.0
ORDER BY b."Id"
LIMIT @p
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Where(t => (t.Products.OrderByDescending(p => p.Price).FirstOrDefault().Price >= 0)).OrderBy(t => t.Id).Select(t => new Brand() {Id = t.Id, Name = t.Name}).Take(3)
```
