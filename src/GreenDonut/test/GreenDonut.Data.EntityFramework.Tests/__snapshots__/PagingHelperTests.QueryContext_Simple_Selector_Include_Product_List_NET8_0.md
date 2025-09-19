# QueryContext_Simple_Selector_Include_Product_List

## SQL 0

```sql
-- @__p_0='3'
SELECT t."Id", t."Name", p."Id", p."Name"
FROM (
    SELECT b."Id", b."Name"
    FROM "Brands" AS b
    ORDER BY b."Id"
    LIMIT @__p_0
) AS t
LEFT JOIN "Products" AS p ON t."Id" = p."BrandId"
ORDER BY t."Id"
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Id).Select(root => new Brand() {Id = root.Id, Name = root.Name, Products = root.Products.Select(p => new Product() {Id = p.Id, Name = p.Name}).ToList()}).Take(3)
```

