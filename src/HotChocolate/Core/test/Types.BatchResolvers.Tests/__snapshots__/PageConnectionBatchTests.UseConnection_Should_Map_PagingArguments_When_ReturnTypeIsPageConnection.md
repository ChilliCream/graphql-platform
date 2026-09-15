# UseConnection_Should_Map_PagingArguments_When_ReturnTypeIsPageConnection

```text
SELECT p."Id", p."Name", p0."Id", p0."BrandId", p0."Name"
FROM "PageConnectionBrands" AS p
LEFT JOIN "PageConnectionProducts" AS p0 ON p."Id" = p0."BrandId"
ORDER BY p."Id"
```
