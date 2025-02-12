# QueryContext_Simple_Selector_Include_Brand_Name

## SQL 0

```sql
-- @__p_0='3'
SELECT t."Id", t."Name", b."Name"
FROM (
    SELECT p."Id", p."BrandId", p."Name"
    FROM "Products" AS p
    ORDER BY p."Id"
    LIMIT @__p_0
) AS t
INNER JOIN "Brands" AS b ON t."BrandId" = b."Id"
ORDER BY t."Id"
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderByDescending(t => t.Id).Select(root => new Product() {Id = root.Id, Name = root.Name, Brand = new Brand() {Name = root.Brand.Name}}).Reverse().Take(3)
```

