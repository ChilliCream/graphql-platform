# UsePaging_Should_Partition_PerAlias_When_FirstDiffers

```text
SELECT c."Id", c."Name", c0."Id", c0."BrandId", c0."Name"
FROM "CursorBrands" AS c
LEFT JOIN "CursorProducts" AS c0 ON c."Id" = c0."BrandId"
ORDER BY c."Id"
```
