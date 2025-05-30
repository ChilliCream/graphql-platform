# QueryContext_Simple_Selector

## SQL 0

```sql
-- @p='3'
SELECT p."Id", p."Name"
FROM "Products" AS p
ORDER BY p."Id"
LIMIT @p
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Id).Select(t => new Product() {Id = t.Id, Name = t.Name}).Take(3)
```

