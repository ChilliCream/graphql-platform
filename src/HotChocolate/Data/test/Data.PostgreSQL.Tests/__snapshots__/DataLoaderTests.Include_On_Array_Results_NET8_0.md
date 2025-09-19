# Include_On_Array_Results

```sql
-- @__brandIds_0={ '1' } (DbType = Object)
SELECT p."BrandId", p."Name", p."Price"
FROM "Products" AS p
WHERE p."BrandId" = ANY (@__brandIds_0)
ORDER BY p."BrandId"
```
