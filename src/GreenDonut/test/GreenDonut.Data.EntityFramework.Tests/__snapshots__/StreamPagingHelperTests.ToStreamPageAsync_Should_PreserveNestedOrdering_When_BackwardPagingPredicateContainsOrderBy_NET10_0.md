# ToStreamPageAsync_Should_PreserveNestedOrdering_When_BackwardPagingPredicateContainsOrderBy

## SQL 0

```sql
-- @p='2'
SELECT b0."Id", b0."Name", EXISTS (
    SELECT 1
    FROM "Brands" AS b1
    WHERE (
        SELECT p0."Price"
        FROM "Products" AS p0
        WHERE b1."Id" = p0."BrandId"
        ORDER BY p0."Price" DESC
        LIMIT 1) >= 0.0
    ORDER BY b1."Id"
    OFFSET 2) AS "HasMore"
FROM (
    SELECT b."Id", b."Name"
    FROM "Brands" AS b
    WHERE (
        SELECT p."Price"
        FROM "Products" AS p
        WHERE b."Id" = p."BrandId"
        ORDER BY p."Price" DESC
        LIMIT 1) >= 0.0
    ORDER BY b."Id"
    LIMIT @p
) AS b0
ORDER BY b0."Id" DESC
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Where(t => (t.Products.OrderByDescending(p => p.Price).FirstOrDefault().Price >= 0)).OrderBy(t => t.Id).Select(t => new Brand() {Id = t.Id, Name = t.Name}).Take(2).OrderByDescending(t => t.Id).Select(t => new StreamRow`1() {Item = t, HasMore = Convert(value(GreenDonut.Data.StreamPagingQueryableExtensions+<>c__DisplayClass2_0`1[GreenDonut.Data.TestContext.Brand]).hasMoreQuery.Any(), Nullable`1)})
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
      "Id": 2,
      "Name": "Brand1",
      "DisplayName": null,
      "AlwaysNull": null,
      "Products": [],
      "BrandDetails": null
    },
    {
      "Id": 1,
      "Name": "Brand0",
      "DisplayName": null,
      "AlwaysNull": null,
      "Products": [],
      "BrandDetails": null
    }
  ],
  "Cursors": [
    "e30y",
    "e30x"
  ]
}
```
