# QueryContext_Simple_Selector_Include_Product_List

## SQL 0

```sql
-- @p='3'
SELECT b0."Id", b0."Name", p."Id", p."Name"
FROM (
    SELECT b."Id", b."Name"
    FROM "Brands" AS b
    ORDER BY b."Id"
    LIMIT @p
) AS b0
LEFT JOIN "Products" AS p ON b0."Id" = p."BrandId"
ORDER BY b0."Id"
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Id).Select(root => new Brand() {Id = root.Id, Name = root.Name, Products = root.Products.Select(p => new Product() {Id = p.Id, Name = p.Name}).ToList()}).Take(3)
```

