# Project_Key_To_Collection_Expression

```text
-- @keys={ '1' } (DbType = Object)
SELECT b."Id", p."Name", p."Id"
FROM "Brands" AS b
LEFT JOIN "Products" AS p ON b."Id" = p."BrandId"
WHERE b."Id" = ANY (@keys)
ORDER BY b."Id"
```
