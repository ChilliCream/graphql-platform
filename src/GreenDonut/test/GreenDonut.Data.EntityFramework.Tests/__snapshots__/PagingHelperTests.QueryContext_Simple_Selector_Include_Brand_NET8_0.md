# QueryContext_Simple_Selector_Include_Brand

## SQL 0

```sql
-- @__p_0='3'
SELECT t."Id", t."Name", b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
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
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderByDescending(t => t.Id).Select(root => new Product() {Id = root.Id, Name = root.Name, Brand = root.Brand}).Reverse().Take(3)
```

