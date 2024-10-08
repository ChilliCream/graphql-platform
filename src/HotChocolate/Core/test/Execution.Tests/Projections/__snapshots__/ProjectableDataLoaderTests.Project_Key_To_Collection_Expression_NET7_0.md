# Project_Key_To_Collection_Expression

```text
SELECT b."Id", p."Name", p."Id"
FROM "Brands" AS b
LEFT JOIN "Products" AS p ON b."Id" = p."BrandId"
WHERE b."Id" = 1
ORDER BY b."Id"
```
