# QueryContext_Simple_Selector

## SQL 0

```sql
-- @__p_0='3'
SELECT p."Id", p."Name"
FROM "Products" AS p
ORDER BY p."Id"
LIMIT @__p_0
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Id).Select(t => new Product() {Id = t.Id, Name = t.Name}).Take(3)
```

