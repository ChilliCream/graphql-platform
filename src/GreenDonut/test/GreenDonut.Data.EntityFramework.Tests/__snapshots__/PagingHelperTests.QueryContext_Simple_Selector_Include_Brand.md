# QueryContext_Simple_Selector_Include_Brand

## SQL 0

```sql
-- @__p_0='3'
SELECT p0."Id", p0."Name", b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
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
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderByDescending(t => t.Id).Select(root => new Product() {Id = root.Id, Name = root.Name, Brand = root.Brand}).Reverse().Take(3)
```

