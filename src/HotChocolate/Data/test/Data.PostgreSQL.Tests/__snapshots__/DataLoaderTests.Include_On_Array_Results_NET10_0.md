# Include_On_Array_Results

```sql
-- @brandIds={ '1' } (DbType = Object)
SELECT p."BrandId", p."Name", p."Price"
FROM "Products" AS p
WHERE p."BrandId" = ANY (@brandIds)
ORDER BY p."BrandId"
```
