# QueryContext_Simple_Selector_Include_Brand_Name

## SQL 0

```sql
-- @__p_0='3'
SELECT p0."Id", p0."Name", b."Name"
FROM (
    SELECT p."Id", p."BrandId", p."Name"
    FROM "Products" AS p
    ORDER BY p."Id"
    LIMIT @__p_0
) AS p0
INNER JOIN "Brands" AS b ON p0."BrandId" = b."Id"
ORDER BY p0."Id"
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Id).Select(root => new Product() {Id = root.Id, Name = root.Name, Brand = new Brand() {Name = root.Brand.Name}}).Take(3)
```

