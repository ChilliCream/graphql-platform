# ToStreamPageAsync_Should_NotHoistInnerOrderProperties_When_BackwardPagingSelectorContainsNestedOrderBy

## SQL 0

```sql
-- @__p_0='2'
SELECT b0."Id", b0."Name", (
    SELECT p."Name"
    FROM "Products" AS p
    WHERE b0."Id" = p."BrandId"
    ORDER BY p."Price" DESC, p."AvailableStock"
    LIMIT 1) AS "DisplayName", EXISTS (
    SELECT 1
    FROM "Brands" AS b1
    ORDER BY b1."Id" DESC
    OFFSET 2) AS "HasMore"
FROM (
    SELECT b."Id", b."Name"
    FROM "Brands" AS b
    ORDER BY b."Id" DESC
    LIMIT @__p_0
) AS b0
ORDER BY b0."Id"
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderByDescending(t => t.Id).Select(root => new Brand() {Id = root.Id, Name = root.Name, DisplayName = root.Products.OrderByDescending(p => p.Price).ThenBy(p => p.AvailableStock).FirstOrDefault().Name}).Take(2).OrderBy(t => t.Id).Select(t => new StreamRow`1() {Item = t, HasMore = Convert(value(GreenDonut.Data.StreamPagingQueryableExtensions+<>c__DisplayClass2_0`1[GreenDonut.Data.TestContext.Brand]).hasMoreQuery.Any(), Nullable`1)})
```

## Result 3

```json
{
  "Index": null,
  "TotalCount": null,
  "HasNextPage": false,
  "HasPreviousPage": true,
  "Items": [
    {
      "Id": 99,
      "Name": "Brand98",
      "DisplayName": "Product 98-0",
      "AlwaysNull": null,
      "Products": [],
      "BrandDetails": null
    },
    {
      "Id": 100,
      "Name": "Brand99",
      "DisplayName": "Product 99-0",
      "AlwaysNull": null,
      "Products": [],
      "BrandDetails": null
    }
  ],
  "Cursors": [
    "e305OQ==",
    "e30xMDA="
  ]
}
```
