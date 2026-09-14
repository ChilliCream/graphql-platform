# UseOffsetPaging_Should_Slice_PerParent_When_FieldIsBatchResolved

```text
SELECT o."Id", o."Name", o0."Id", o0."BrandId", o0."Name"
FROM "OffsetBrands" AS o
LEFT JOIN "OffsetProducts" AS o0 ON o."Id" = o0."BrandId"
ORDER BY o."Id"
```
