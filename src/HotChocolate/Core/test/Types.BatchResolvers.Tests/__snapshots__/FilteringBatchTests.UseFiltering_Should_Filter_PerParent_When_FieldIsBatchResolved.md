# UseFiltering_Should_Filter_PerParent_When_FieldIsBatchResolved

```text
SELECT f."Id", f."Name", f0."Id", f0."BrandId", f0."Name"
FROM "FilteringBrands" AS f
LEFT JOIN "FilteringProducts" AS f0 ON f."Id" = f0."BrandId"
ORDER BY f."Id"
```
