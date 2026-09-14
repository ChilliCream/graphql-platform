# UseSorting_Should_Order_PerParent_When_FieldIsBatchResolved

```text
SELECT s."Id", s."Name", s0."Id", s0."BrandId", s0."Name"
FROM "SortingBrands" AS s
LEFT JOIN "SortingProducts" AS s0 ON s."Id" = s0."BrandId"
ORDER BY s."Id"
```
